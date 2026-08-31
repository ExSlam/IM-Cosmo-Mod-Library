using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SaveNLoadFixes.Safety;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Vanilla enqueue seam. DoBirthday may synchronously open the first popup, so
    /// the postfix synchronizes the queue blocker after the authoritative Queue.Add.
    /// No checkpoint can occur inside that synchronous method body.
    /// </summary>
    [HarmonyPatch]
    internal static class BirthdayDoBirthday_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Birthday),
                nameof(Birthday.DoBirthday),
                new Type[] { typeof(data_girls.girls) });
            if (method == null)
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "Birthday.DoBirthday(data_girls.girls)",
                    "method could not be resolved");
                return null;
            }

            BirthdayCheckpointPatchHealth.ReportDoBirthdayResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            BirthdayCheckpointRepair.SyncQueueBlocker("Birthday.DoBirthday");
        }
    }

    /// <summary>
    /// Vanilla dequeue seam. PopupManager.Close_ invokes this callback in the same
    /// tween-completion callback that releases popup protection; after Queue.Remove
    /// and MoveQueue return, a zero queue may safely release SNLF's blocker.
    /// </summary>
    [HarmonyPatch]
    internal static class BirthdayPopupCloseCallback_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Birthday_Popup),
                "<OnClose>b__30_0",
                Type.EmptyTypes);
            if (method == null)
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "Birthday_Popup.<OnClose>b__30_0()",
                    "close callback could not be resolved");
                return null;
            }

            BirthdayCheckpointPatchHealth.ReportCloseCallbackResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            BirthdayCheckpointRepair.SyncQueueBlocker("Birthday_Popup.OnClose callback");
        }
    }

    /// <summary>
    /// Registers the exact delayed iterator returned by Birthday._MoveQueue().
    /// </summary>
    [HarmonyPatch]
    internal static class BirthdayMoveQueueFactory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(Birthday),
                "_MoveQueue",
                Type.EmptyTypes);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "Birthday._MoveQueue()",
                    "IEnumerator factory could not be resolved");
                return null;
            }

            BirthdayCheckpointPatchHealth.ReportIteratorFactoryResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(ref IEnumerator __result)
        {
            BirthdayCheckpointRepair.RegisterMoveQueue(__result);
        }
    }

    /// <summary>
    /// Selective epoch guard for the single generated birthday queue iterator. A
    /// discarded iterator returns false before it can reread Birthday.Queue[0].
    /// </summary>
    [HarmonyPatch]
    internal static class BirthdayMoveQueueMoveNext_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(Birthday).GetNestedType(
                "<_MoveQueue>d__4",
                BindingFlags.NonPublic);
            if (iteratorType == null)
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "Birthday.<_MoveQueue>d__4",
                    "generated iterator type could not be resolved");
                return null;
            }

            MethodInfo[] methods = iteratorType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                bool nameMatches =
                    string.Equals(method.Name, "MoveNext", StringComparison.Ordinal) ||
                    method.Name.EndsWith(".MoveNext", StringComparison.Ordinal);
                if (nameMatches &&
                    method.ReturnType == typeof(bool) &&
                    method.GetParameters().Length == 0)
                {
                    BirthdayCheckpointPatchHealth.ReportMoveNextResolved();
                    return method;
                }
            }

            BirthdayCheckpointPatchHealth.ReportFailure(
                "Birthday.<_MoveQueue>d__4.MoveNext",
                "zero-argument bool MoveNext could not be resolved");
            return null;
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (BirthdayCheckpointRepair.ShouldRunMoveNext(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, bool __result)
        {
            if (!__result)
            {
                BirthdayCheckpointRepair.MarkMoveNextCompleted(__instance);
            }
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception != null)
            {
                BirthdayCheckpointRepair.MarkMoveNextFaulted(__instance, __exception);
            }

            return __exception;
        }
    }

    /// <summary>
    /// Clears process-local Birthday.Queue references before data_girls replaces the
    /// old idol objects with the target save's reconstructed roster.
    /// </summary>
    [HarmonyPatch]
    internal static class BirthdayDataGirlsLoad_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(data_girls),
                nameof(data_girls.LoadFunction),
                Type.EmptyTypes);
            if (method == null)
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "data_girls.LoadFunction()",
                    "load seam could not be resolved");
                return null;
            }

            BirthdayCheckpointPatchHealth.ReportDataGirlsLoadResolved();
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            BirthdayCheckpointRepair.ClearStaleQueueBeforeGirlRebuild();
        }
    }

    /// <summary>
    /// Runs compatibility reconstruction only after a successful career load has
    /// fully returned from SaveManager.LoadEvent. The epoch snapshot distinguishes a
    /// real successful target adoption from Task-1-blocked or failed LoadData calls.
    /// </summary>
    [HarmonyPatch]
    internal static class BirthdayCareerLoadCompatibility_SaveNLoadFixes_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo pathLoad = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(string) });
            MethodInfo autoLoad = AccessTools.Method(
                typeof(SaveManager),
                nameof(SaveManager.LoadData),
                new Type[] { typeof(bool) });

            if (pathLoad == null)
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "SaveManager.LoadData(string)",
                    "career load could not be resolved");
            }
            else
            {
                BirthdayCheckpointPatchHealth.ReportCareerLoadResolved(pathLoad);
                yield return pathLoad;
            }

            if (autoLoad == null)
            {
                BirthdayCheckpointPatchHealth.ReportFailure(
                    "SaveManager.LoadData(bool)",
                    "career load could not be resolved");
            }
            else
            {
                BirthdayCheckpointPatchHealth.ReportCareerLoadResolved(autoLoad);
                yield return autoLoad;
            }
        }

        [HarmonyPrefix]
        private static void Prefix(out long __state)
        {
            __state = LoadEpoch.Current;
        }

        [HarmonyPostfix]
        private static void Postfix(SaveManager __instance, long __state)
        {
            BirthdayCheckpointRepair.TryReconstructLegacyQueueAfterCareerLoad(
                __instance,
                __state);
        }
    }
}

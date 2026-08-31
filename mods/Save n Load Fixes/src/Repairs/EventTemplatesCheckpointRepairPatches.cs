using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Registers the exact generated iterator returned by Event_Templates.OpenPopup.
    /// This factory is reached synchronously from _OpenPopup() only after template
    /// selection/building has succeeded.
    /// </summary>
    [HarmonyPatch]
    internal static class EventTemplatesOpenPopupFactory_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = typeof(Event_Templates).GetMethod(
                "OpenPopup",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                EventTemplatesCheckpointPatchHealth.ReportFailure(
                    "Event_Templates.OpenPopup()",
                    "private IEnumerator factory could not be resolved");
                throw new MissingMethodException(
                    typeof(Event_Templates).FullName,
                    "OpenPopup");
            }

            EventTemplatesCheckpointPatchHealth.ReportFactoryResolved();
            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(ref IEnumerator __result)
        {
            EventTemplatesCheckpointRepair.RegisterPendingOpen(__result);
        }
    }

    /// <summary>
    /// Guards only Event_Templates.OpenPopup's generated iterator against discarded
    /// load epochs and injects one exact handoff immediately after PopupManager.Open.
    /// No other coroutine or global Unity scheduler surface is touched.
    /// </summary>
    [HarmonyPatch]
    internal static class EventTemplatesOpenPopupMoveNext_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            Type iteratorType = typeof(Event_Templates).GetNestedType(
                "<OpenPopup>d__11",
                BindingFlags.NonPublic);

            if (iteratorType == null)
            {
                EventTemplatesCheckpointPatchHealth.ReportFailure(
                    "Event_Templates.<OpenPopup>d__11",
                    "generated iterator type could not be resolved");
                throw new MissingMemberException(
                    typeof(Event_Templates).FullName,
                    "<OpenPopup>d__11");
            }

            MethodInfo moveNext = FindMoveNext(iteratorType);
            if (moveNext == null)
            {
                EventTemplatesCheckpointPatchHealth.ReportFailure(
                    "Event_Templates.<OpenPopup>d__11.MoveNext",
                    "zero-argument bool MoveNext method could not be resolved");
                throw new MissingMethodException(
                    iteratorType.FullName,
                    "MoveNext");
            }

            EventTemplatesCheckpointPatchHealth.ReportMoveNextResolved();
            return moveNext;
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (EventTemplatesCheckpointRepair.ShouldRunMoveNext(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            List<CodeInstruction> original =
                new List<CodeInstruction>(instructions);
            MethodInfo popupOpen = AccessTools.Method(
                typeof(PopupManager),
                nameof(PopupManager.Open),
                new Type[] { typeof(PopupManager._type), typeof(bool) });
            MethodInfo handoff = AccessTools.Method(
                typeof(EventTemplatesCheckpointRepair),
                nameof(EventTemplatesCheckpointRepair.MarkPopupOpened),
                new Type[] { typeof(object) });

            if (popupOpen == null || handoff == null)
            {
                EventTemplatesCheckpointPatchHealth.ReportFailure(
                    "Event_Templates.<OpenPopup>d__11.MoveNext handoff",
                    "PopupManager.Open or SNLF handoff method could not be resolved");
                return original;
            }

            int observed = 0;
            foreach (CodeInstruction instruction in original)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if (called != null && called == popupOpen)
                {
                    observed++;
                }
            }

            EventTemplatesCheckpointPatchHealth.ReportMoveNextOpenSites(
                __originalMethod,
                observed);

            if (observed != 1)
            {
                // The registration path checks patch health before taking a blocker,
                // so returning unmodified IL here cannot strand a checkpoint lease.
                return original;
            }

            List<CodeInstruction> patched = new List<CodeInstruction>();
            foreach (CodeInstruction instruction in original)
            {
                patched.Add(instruction);

                MethodInfo called = instruction.operand as MethodInfo;
                if (called == null || called != popupOpen)
                {
                    continue;
                }

                // PopupManager.Open returns void. Immediately after it returns,
                // ldarg.0 pushes the iterator instance used as the exact registry key.
                patched.Add(new CodeInstruction(OpCodes.Ldarg_0));
                patched.Add(
                    new CodeInstruction(
                        OpCodes.Call,
                        handoff));
            }

            return patched;
        }

        private static MethodInfo FindMoveNext(Type iteratorType)
        {
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
                    return method;
                }
            }

            return null;
        }
    }
}

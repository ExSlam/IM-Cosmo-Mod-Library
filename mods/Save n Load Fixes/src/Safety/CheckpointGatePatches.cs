using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Safety
{
    /// <summary>
    /// Autosave already owns the vanilla popup/dialogue/PauseAutosave/main-menu
    /// checks. SNLF adds only its own repair-blocker condition to that result.
    /// </summary>
    [HarmonyPatch]
    internal static class CheckpointGateCanAutosave_SaveNLoadFixes_Patch
    {
        private static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method(
                typeof(SaveManager),
                "CanAutosave",
                Type.EmptyTypes);

            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    "CanAutosave");
            }

            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (__result && CheckpointGate.HasActiveBlockers)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// F5 calls SaveData(false, true) directly and bypasses CanAutosave().
    /// Enforce the vanilla transient rules plus SNLF blockers for that manual
    /// SaveData path without changing normal autosave call semantics.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.SaveData),
        new Type[] { typeof(bool), typeof(bool) })]
    internal static class CheckpointGateManualSave_SaveNLoadFixes_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(bool autoSave)
        {
            if (autoSave)
            {
                return true;
            }

            string reason;
            if (CheckpointGate.ShouldAllowManualSave(out reason))
            {
                return true;
            }

            CheckpointGate.RecordBlockedOperation("Manual save/F5", reason);
            return false;
        }
    }

    /// <summary>
    /// Reject unsafe same-process gameplay loads while preserving main-menu
    /// and loading-screen use of the same SaveManager overloads.
    /// </summary>
    [HarmonyPatch]
    internal static class CheckpointGateInGameLoad_SaveNLoadFixes_Patch
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
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    "LoadData(string)");
            }

            if (autoLoad == null)
            {
                throw new MissingMethodException(
                    typeof(SaveManager).FullName,
                    "LoadData(bool)");
            }

            yield return pathLoad;
            yield return autoLoad;
        }

        [HarmonyPrefix]
        private static bool Prefix()
        {
            string reason;
            if (CheckpointGate.ShouldAllowInGameLoad(out reason))
            {
                return true;
            }

            CheckpointGate.RecordBlockedOperation("In-game load/F9", reason);
            return false;
        }
    }
}

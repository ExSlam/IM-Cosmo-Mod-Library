using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Bootstrap only. Resolving this always-available vanilla target initializes the
    /// optional FWS compatibility layer during SNLF patch installation. The layer then
    /// patches the audited external FWS postfix with SNLF's own Harmony owner, either
    /// immediately or when that exact assembly is loaded later.
    /// </summary>
    [HarmonyPatch]
    internal static class FansWatchShows_WideNumericBootstrap_Patch
    {
        private static MethodBase TargetMethod()
        {
            FansWatchShowsWideNumericInterop.EnsureInitialized();
            MethodInfo method = AccessTools.Method(typeof(Mods), nameof(Mods.LoadMods), Type.EmptyTypes);
            if (method == null)
                throw new MissingMethodException(typeof(Mods).FullName, nameof(Mods.LoadMods));
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            // Initialization occurs during TargetMethod resolution. This no-op prefix
            // keeps the bootstrap attached to a stable vanilla method so Harmony runs
            // the resolver whenever SNLF itself is reapplied.
        }
    }
}

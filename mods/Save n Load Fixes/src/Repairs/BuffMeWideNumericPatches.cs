using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Bootstrap for the optional BuffMe 1.0.0 compatibility profile. Initialization
    /// occurs during SNLF patch discovery and the interop layer also watches later
    /// assembly loads, so mod load order is not a compatibility requirement.
    /// </summary>
    [HarmonyPatch]
    internal static class BuffMe_WideNumericBootstrap_Patch
    {
        private static MethodBase TargetMethod()
        {
            BuffMeWideNumericInterop.EnsureInitialized();
            MethodInfo method = AccessTools.Method(typeof(Mods), nameof(Mods.LoadMods), Type.EmptyTypes);
            if (method == null)
                throw new MissingMethodException(typeof(Mods).FullName, nameof(Mods.LoadMods));
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix() { }
    }
}

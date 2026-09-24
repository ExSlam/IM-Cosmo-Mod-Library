using System;
using System.Reflection;
using HarmonyLib;

namespace SaveNLoadFixes.Repairs
{
    [HarmonyPatch]
    internal static class TbsBalancePatch_WideNumericBootstrap_Patch
    {
        private static MethodBase TargetMethod()
        {
            TbsBalancePatchWideNumericInterop.EnsureInitialized();
            MethodInfo method = AccessTools.Method(typeof(Mods), nameof(Mods.LoadMods), Type.EmptyTypes);
            if (method == null) throw new MissingMethodException(typeof(Mods).FullName, nameof(Mods.LoadMods));
            return method;
        }

        [HarmonyPrefix]
        private static void Prefix() { }
    }
}

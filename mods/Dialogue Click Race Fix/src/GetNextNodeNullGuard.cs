using System;
using HarmonyLib;

namespace DialogueClickRaceFix
{
    /// <summary>
    /// Defense in depth only. The race itself is stopped in OnScreenClick before
    /// Next() is entered. If some other caller nevertheless asks vanilla traversal
    /// for the node after null, the correct result is simply no next node.
    /// </summary>
    [HarmonyPatch(
        typeof(data_dialogues),
        nameof(data_dialogues.GetNextNode),
        new Type[] { typeof(data_dialogues._dialogue._node) })]
    internal static class GetNextNodeNullGuard
    {
        private static bool Prefix(
            data_dialogues._dialogue._node node,
            ref data_dialogues._dialogue._node __result)
        {
            if (node != null)
            {
                return true;
            }

            __result = null;
            return false;
        }
    }
}

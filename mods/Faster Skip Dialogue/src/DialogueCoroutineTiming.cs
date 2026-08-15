using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FasterSkipDialogue
{
    /// <summary>
    /// Wraps controller-owned coroutines without rewriting their state machines. When fast skip
    /// is active, fixed waits become short realtime waits; WaitUntil and other yield instructions
    /// remain untouched so resource loading and predicate-based sequencing stay correct.
    /// </summary>
    internal static class DialogueCoroutineTiming
    {
        internal static IEnumerator Wrap(ActiveDialogueController controller, IEnumerator original)
        {
            if (original == null)
            {
                yield break;
            }

            try
            {
                while (original.MoveNext())
                {
                    object yielded = original.Current;

                    if (FasterSkipRuntime.ShouldCompressTimedYield(controller) &&
                        (yielded is WaitForSeconds || yielded is WaitForSecondsRealtime))
                    {
                        yield return new WaitForSecondsRealtime(FasterSkipRuntime.CompressedTimedYieldSeconds);
                    }
                    else
                    {
                        yield return yielded;
                    }
                }
            }
            finally
            {
                IDisposable disposable = original as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Applies the timed-yield wrapper to every IEnumerator factory declared directly on
    /// ActiveDialogueController except _Skip(), which is replaced by FasterSkipLoop instead.
    /// This automatically covers vanilla EnableClick, WaitTillSpritesLoad, RenderMask,
    /// DoBefore/AfterDialogue and Finale_Rival_Script without patching compiler-generated
    /// iterator classes by name.
    /// </summary>
    [HarmonyPatch]
    internal static class ActiveDialogueControllerTimedCoroutinePatch
    {
        private const string VanillaSkipCoroutineName = "_Skip";

        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(ActiveDialogueController).GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.IsStatic)
                {
                    continue;
                }

                if (string.Equals(method.Name, VanillaSkipCoroutineName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (method.ReturnType == typeof(IEnumerator))
                {
                    yield return method;
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ActiveDialogueController __instance, ref IEnumerator __result)
        {
            if (__result != null)
            {
                __result = DialogueCoroutineTiming.Wrap(__instance, __result);
            }
        }
    }
}

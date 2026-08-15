using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace FasterSkipDialogue
{
    /// <summary>
    /// Runtime policy for the replacement skip loop.
    ///
    /// The mod deliberately does not alter Unity Time.timeScale. Idol Manager pauses its own
    /// simulation during VN scenes, but other mods may still use Unity time for unrelated work.
    /// Visual acceleration is therefore scoped to DOTween and controller-owned timed yields.
    /// </summary>
    internal static class FasterSkipRuntime
    {
        internal const float StepIntervalSeconds = 0.025f;
        internal const float GatePollSeconds = 0.01f;
        internal const float CompressedTimedYieldSeconds = 0.02f;
        internal const float EndTransitionGraceSeconds = 0.25f;
        internal const float TweenSpeedMultiplier = 30f;

        private static ActiveDialogueController owner;
        private static int runnerGeneration;
        private static bool sceneEnding;

        private static bool tweenBoostActive;
        private static float tweenScaleBeforeBoost = 1f;
        private static float tweenScaleApplied = 1f;

        internal static int BeginRunner(ActiveDialogueController controller)
        {
            if (controller == null)
            {
                return -1;
            }

            if (owner != null && owner != controller)
            {
                ForceCleanup();
            }

            owner = controller;
            sceneEnding = false;
            runnerGeneration++;
            return runnerGeneration;
        }

        internal static bool IsCurrentRunner(ActiveDialogueController controller, int generation)
        {
            return controller != null &&
                   owner == controller &&
                   generation == runnerGeneration;
        }

        internal static bool IsChoiceBoundary(ActiveDialogueController controller)
        {
            return controller != null &&
                   controller.activeNode != null &&
                   controller.activeNode.type == data_dialogues._dialogue._node._type.choice;
        }

        internal static bool IsInteractiveBoundary(ActiveDialogueController controller)
        {
            // ShowingPopup is treated as choice-equivalent. Vanilla Update() itself refuses to
            // feed keyboard progression into OnScreenClick while a VN popup is open. Advancing
            // behind an interactive popup can skip required player input and corrupt story flow.
            return IsChoiceBoundary(controller) || ActiveDialogueController.ShowingPopup;
        }

        internal static bool IsSceneEnding(ActiveDialogueController controller)
        {
            return controller != null && owner == controller && sceneEnding;
        }

        internal static bool ShouldFastRun(ActiveDialogueController controller)
        {
            return controller != null &&
                   owner == controller &&
                   !sceneEnding &&
                   ActiveDialogueController.Skip &&
                   ActiveDialogueController.ShowingDialogue &&
                   !IsInteractiveBoundary(controller);
        }

        internal static bool ShouldCompressTimedYield(ActiveDialogueController controller)
        {
            if (controller == null || owner != controller)
            {
                return false;
            }

            if (sceneEnding)
            {
                return true;
            }

            return ActiveDialogueController.Skip &&
                   ActiveDialogueController.ShowingDialogue &&
                   !IsInteractiveBoundary(controller);
        }

        internal static bool ShouldProtectSkipState()
        {
            return owner != null &&
                   !sceneEnding &&
                   ActiveDialogueController.Skip &&
                   ActiveDialogueController.ShowingDialogue;
        }

        internal static void RestoreProtectedSkipState(bool wasProtected)
        {
            if (!wasProtected || owner == null || sceneEnding || !ActiveDialogueController.ShowingDialogue)
            {
                return;
            }

            ActiveDialogueController.Skip = true;
        }

        internal static void MarkSceneEnding(ActiveDialogueController controller)
        {
            if (controller == null || owner != controller)
            {
                return;
            }

            // Hide() is the authoritative VN-scene end. Turn the vanilla flag off immediately so
            // the next dialogue cannot inherit it, but keep the visual boost alive briefly so the
            // closing fades complete quickly too.
            sceneEnding = true;
            ActiveDialogueController.Skip = false;
            EngageTweenBoost();
        }

        internal static void ResetForNewDialogue(ActiveDialogueController controller)
        {
            // Set() is used for a new top-level dialogue. Internal instant_transition does not call
            // Set(), so skip intentionally survives those data-driven cutscene segments. A new Set
            // always owns a fresh skip session, even if a scene change created a new controller.
            ForceCleanup();
        }

        internal static void ForceCleanup()
        {
            RestoreTweenBoost();
            owner = null;
            sceneEnding = false;
            runnerGeneration++;
        }

        internal static void PauseVisualAcceleration()
        {
            RestoreTweenBoost();
        }

        internal static void EngageTweenBoost()
        {
            float currentScale = DOTween.timeScale;

            // Respect an external hard pause. Do not silently unpause another mod's tween system.
            if (currentScale <= 0f)
            {
                return;
            }

            if (!tweenBoostActive)
            {
                tweenScaleBeforeBoost = currentScale;
            }
            else if (!Mathf.Approximately(currentScale, tweenScaleApplied))
            {
                // Another mod changed DOTween.timeScale while Faster Skip was active. Treat that
                // new value as the baseline instead of restoring an obsolete value later.
                tweenScaleBeforeBoost = currentScale;
            }

            tweenScaleApplied = tweenScaleBeforeBoost * TweenSpeedMultiplier;
            DOTween.timeScale = tweenScaleApplied;
            tweenBoostActive = true;
        }

        internal static void RestoreTweenBoost()
        {
            if (!tweenBoostActive)
            {
                return;
            }

            // Avoid overwriting an external change made after our last application.
            if (Mathf.Approximately(DOTween.timeScale, tweenScaleApplied))
            {
                DOTween.timeScale = tweenScaleBeforeBoost;
            }

            tweenBoostActive = false;
            tweenScaleApplied = DOTween.timeScale;
            tweenScaleBeforeBoost = DOTween.timeScale;
        }
    }

    /// <summary>
    /// Replacement for ActiveDialogueController._Skip(). It advances through the same public
    /// OnScreenClick path the player uses, so checks, actions, logs, modded effects, choices,
    /// instant transitions and Harmony patches continue to execute normally.
    /// </summary>
    internal static class FasterSkipLoop
    {
        internal static IEnumerator Run(ActiveDialogueController controller)
        {
            int generation = FasterSkipRuntime.BeginRunner(controller);
            if (generation < 0)
            {
                yield break;
            }

            try
            {
                while (FasterSkipRuntime.IsCurrentRunner(controller, generation))
                {
                    if (FasterSkipRuntime.IsSceneEnding(controller))
                    {
                        // Hide() has already disarmed Skip. Keep acceleration only for a bounded
                        // closing window so ordinary 1-4 second VN fades finish quickly, then
                        // restore global tween state even if a chapter-title/post-scene prompt
                        // deliberately keeps ShowingDialogue true awaiting player input.
                        FasterSkipRuntime.EngageTweenBoost();
                        yield return new WaitForSecondsRealtime(FasterSkipRuntime.EndTransitionGraceSeconds);
                        yield break;
                    }

                    if (!ActiveDialogueController.Skip || !ActiveDialogueController.ShowingDialogue)
                    {
                        yield break;
                    }

                    if (FasterSkipRuntime.IsInteractiveBoundary(controller))
                    {
                        // Skip stays armed at a choice. Only the acceleration is paused. Once the
                        // choice changes activeNode, this same coroutine resumes automatically.
                        FasterSkipRuntime.PauseVisualAcceleration();
                        yield return null;
                        continue;
                    }

                    FasterSkipRuntime.EngageTweenBoost();

                    // A null activeNode is valid while a VN is starting or changing internal
                    // segments. Never feed that state into vanilla GetNextNode(null).
                    if (controller.activeNode == null)
                    {
                        yield return new WaitForSecondsRealtime(FasterSkipRuntime.GatePollSeconds);
                        continue;
                    }

                    // Preserve state ordering. We accelerate the mechanisms that clear these
                    // gates, but do not bypass the gates themselves.
                    if (controller.Transitioning ||
                        controller.DisableClick ||
                        ActiveDialogueController.IsLoadingBG)
                    {
                        yield return new WaitForSecondsRealtime(FasterSkipRuntime.GatePollSeconds);
                        continue;
                    }

                    vn_text dialogueText = controller.textBox != null
                        ? controller.textBox.GetComponent<vn_text>()
                        : null;

                    if (dialogueText != null && dialogueText.animatingText)
                    {
                        dialogueText.StopAnimation();
                    }

                    controller.OnScreenClick();
                    yield return new WaitForSecondsRealtime(FasterSkipRuntime.StepIntervalSeconds);
                }
            }
            finally
            {
                // If this runner was superseded by a newly enabled runner, the new owner of the
                // generation also owns the boost. Otherwise restore immediately.
                if (FasterSkipRuntime.IsCurrentRunner(controller, generation))
                {
                    FasterSkipRuntime.ForceCleanup();
                }
            }
        }
    }
}

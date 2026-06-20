using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IMUiFramework
{
    /// <summary>
    /// Small, UI-focused wrappers around Idol Manager's bundled DOTween runtime.
    /// All methods use unscaled time by default so animations continue while the
    /// game's popup system has paused simulation.
    /// </summary>
    public static class IMUiTween
    {
        private const float MinimumDuration = 0f;

        public static Tween Fade(
            CanvasGroup target,
            float endAlpha,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOFade(target, Mathf.Clamp01(endAlpha), NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween Fade(
            Graphic target,
            float endAlpha,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOFade(target, Mathf.Clamp01(endAlpha), NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween Fade(
            TMP_Text target,
            float endAlpha,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                ShortcutExtensionsTMPText.DOFade(target, Mathf.Clamp01(endAlpha), NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween Color(
            Graphic target,
            Color endColor,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOColor(target, endColor, NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween Color(
            TMP_Text target,
            Color endColor,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                ShortcutExtensionsTMPText.DOColor(target, endColor, NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween MoveAnchored(
            RectTransform target,
            Vector2 endPosition,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            Vector3 endPosition3D = target.anchoredPosition3D;
            endPosition3D.x = endPosition.x;
            endPosition3D.y = endPosition.y;
            return Configure(
                DOTweenModuleUI.DOAnchorPos3D(target, endPosition3D, NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween Resize(
            RectTransform target,
            Vector2 endSize,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOSizeDelta(target, endSize, NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween ResizeMinimum(
            LayoutElement target,
            Vector2 endSize,
            float duration,
            Ease ease = Ease.OutQuad,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOMinSize(target, endSize, NormalizeDuration(duration)),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static Tween PunchAnchored(
            RectTransform target,
            Vector2 punch,
            float duration,
            int vibrato = 10,
            float elasticity = 1f,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOPunchAnchorPos(
                    target,
                    punch,
                    NormalizeDuration(duration),
                    Mathf.Max(1, vibrato),
                    Mathf.Clamp01(elasticity)),
                Ease.Linear,
                useUnscaledTime,
                onComplete);
        }

        public static Tween ShakeAnchored(
            RectTransform target,
            float duration,
            Vector2 strength,
            int vibrato = 10,
            float randomness = 90f,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                DOTweenModuleUI.DOShakeAnchorPos(
                    target,
                    NormalizeDuration(duration),
                    strength,
                    Mathf.Max(1, vibrato),
                    Mathf.Clamp(randomness, 0f, 180f)),
                Ease.Linear,
                useUnscaledTime,
                onComplete);
        }

        public static Tween RevealText(
            TMP_Text target,
            string value,
            float duration,
            bool richTextEnabled = true,
            Ease ease = Ease.Linear,
            bool useUnscaledTime = true,
            TweenCallback onComplete = null)
        {
            if (target == null)
            {
                return null;
            }

            return Configure(
                ShortcutExtensionsTMPText.DOText(
                    target,
                    value ?? string.Empty,
                    NormalizeDuration(duration),
                    richTextEnabled),
                ease,
                useUnscaledTime,
                onComplete);
        }

        public static void Kill(object target, bool complete = false)
        {
            if (target == null)
            {
                return;
            }

            DOTween.Kill(target, complete);
        }

        private static Tween Configure(
            Tween tween,
            Ease ease,
            bool useUnscaledTime,
            TweenCallback onComplete)
        {
            if (tween == null)
            {
                return null;
            }

            TweenSettingsExtensions.SetEase<Tween>(tween, ease);
            TweenSettingsExtensions.SetUpdate<Tween>(tween, useUnscaledTime);
            if (onComplete != null)
            {
                TweenSettingsExtensions.OnComplete<Tween>(tween, onComplete);
            }

            return tween;
        }

        private static float NormalizeDuration(float duration)
        {
            return Mathf.Max(MinimumDuration, duration);
        }
    }
}

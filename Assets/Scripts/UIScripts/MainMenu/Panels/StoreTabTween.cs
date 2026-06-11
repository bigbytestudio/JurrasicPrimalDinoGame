using System;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Optional DOTween scale helper. Uses reflection so the project compiles before DOTween is imported.
    /// </summary>
    public static class StoreTabTween
    {
        private static bool checkedAvailability;
        private static bool isAvailable;

        public static bool IsDotweenAvailable
        {
            get
            {
                if (!checkedAvailability)
                {
                    isAvailable = Type.GetType("DG.Tweening.DOTween, DOTween") != null;
                    checkedAvailability = true;
                }

                return isAvailable;
            }
        }

        public static void AnimateScale(RectTransform target, float scale, float duration, bool useDotween)
        {
            if (target == null)
                return;

            if (!useDotween || !IsDotweenAvailable)
            {
                target.localScale = Vector3.one * scale;
                return;
            }

            Kill(target);

            Type extensionsType = Type.GetType("DG.Tweening.ShortcutExtensions, DOTween");
            if (extensionsType == null)
            {
                target.localScale = Vector3.one * scale;
                return;
            }

            var doScale = extensionsType.GetMethod(
                "DOScale",
                new[] { typeof(Transform), typeof(float), typeof(float) });

            if (doScale == null)
            {
                target.localScale = Vector3.one * scale;
                return;
            }

            object tween = doScale.Invoke(null, new object[] { target, scale, duration });
            ApplyEase(tween, "OutBack");
        }

        public static void Kill(RectTransform target)
        {
            if (target == null || !IsDotweenAvailable)
                return;

            Type dotweenType = Type.GetType("DG.Tweening.DOTween, DOTween");
            dotweenType?.GetMethod("Kill", new[] { typeof(object), typeof(bool) })
                ?.Invoke(null, new object[] { target, false });
        }

        private static void ApplyEase(object tween, string easeName)
        {
            if (tween == null)
                return;

            Type easeType = Type.GetType("DG.Tweening.Ease, DOTween");
            if (easeType == null)
                return;

            object easeValue = Enum.Parse(easeType, easeName);
            tween.GetType().GetMethod("SetEase", new[] { easeType })?.Invoke(tween, new[] { easeValue });
        }
    }
}

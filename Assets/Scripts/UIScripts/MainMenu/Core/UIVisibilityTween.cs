using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    public static class UIVisibilityTween
    {
        private static readonly Dictionary<RectTransform, Vector3> ShownScales = new();

        public static void SetVisible(
            Component target,
            bool visible,
            float duration = 0.22f,
            float hiddenScaleMultiplier = 0.92f,
            bool immediate = false,
            Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            SetVisible(
                target.transform as RectTransform,
                visible,
                duration,
                hiddenScaleMultiplier,
                immediate,
                onComplete);
        }

        public static void SetVisible(
            RectTransform rect,
            bool visible,
            float duration = 0.22f,
            float hiddenScaleMultiplier = 0.92f,
            bool immediate = false,
            Action onComplete = null)
        {
            if (rect == null)
            {
                onComplete?.Invoke();
                return;
            }

            DOTween.Kill(rect);

            if (immediate)
            {
                ApplyImmediate(rect, visible);
                onComplete?.Invoke();
                return;
            }

            bool alreadyVisible = rect.gameObject.activeInHierarchy
                && rect.TryGetComponent(out CanvasGroup existingGroup)
                && existingGroup.alpha > 0.01f;

            if (visible && alreadyVisible)
            {
                onComplete?.Invoke();
                return;
            }

            if (!visible && !rect.gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return;
            }

            CanvasGroup group = GetOrAddCanvasGroup(rect);
            CacheShownScale(rect);
            Vector3 shownScale = ShownScales[rect];

            if (visible)
            {
                rect.gameObject.SetActive(true);
                group.alpha = 0f;
                rect.localScale = shownScale * hiddenScaleMultiplier;
                group.interactable = false;
                group.blocksRaycasts = false;

                DOTween.Sequence()
                    .SetTarget(rect)
                    .Join(group.DOFade(1f, duration).SetEase(Ease.OutQuad))
                    .Join(rect.DOScale(shownScale, duration).SetEase(Ease.OutBack))
                    .OnComplete(() =>
                    {
                        group.interactable = true;
                        group.blocksRaycasts = true;
                        onComplete?.Invoke();
                    });
                return;
            }

            group.interactable = false;
            group.blocksRaycasts = false;

            DOTween.Sequence()
                .SetTarget(rect)
                .Join(group.DOFade(0f, duration).SetEase(Ease.InQuad))
                .Join(rect.DOScale(shownScale * hiddenScaleMultiplier, duration).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    rect.gameObject.SetActive(false);
                    rect.localScale = shownScale;
                    group.alpha = 1f;
                    onComplete?.Invoke();
                });
        }

        public static void Stop(Component target)
        {
            if (target == null)
                return;

            DOTween.Kill(target.transform);
        }

        private static void ApplyImmediate(RectTransform rect, bool visible)
        {
            CanvasGroup group = GetOrAddCanvasGroup(rect);
            CacheShownScale(rect);
            Vector3 shownScale = ShownScales[rect];

            rect.gameObject.SetActive(visible);
            rect.localScale = shownScale;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private static void CacheShownScale(RectTransform rect)
        {
            if (ShownScales.ContainsKey(rect))
                return;

            ShownScales[rect] = rect.localScale;
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
        {
            if (!rect.TryGetComponent(out CanvasGroup group))
                group = rect.gameObject.AddComponent<CanvasGroup>();

            return group;
        }
    }
}

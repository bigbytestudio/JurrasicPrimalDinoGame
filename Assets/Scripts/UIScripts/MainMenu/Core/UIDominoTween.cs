using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public enum UIStaggerStyle
    {
        Domino,
        SlideUp,
        FadeScale
    }

    public struct UIStaggerConfig
    {
        public UIStaggerStyle style;
        public float stagger;
        public float duration;
        public float slideOffsetY;
        public float firstItemSlideOffsetY;
        public bool firstItemSlidesFromAbove;
        public float startScale;
        public float startRotationZ;

        public static UIStaggerConfig Domino(float stagger = 0.08f, float duration = 0.4f, float startRotationZ = 18f)
        {
            return new UIStaggerConfig
            {
                style = UIStaggerStyle.Domino,
                stagger = stagger,
                duration = duration,
                slideOffsetY = 0f,
                startScale = 0f,
                startRotationZ = startRotationZ
            };
        }

        public static UIStaggerConfig SlideUp(
            float stagger = 0.06f,
            float duration = 0.42f,
            float slideOffsetY = 36f,
            float startScale = 0.94f,
            float firstItemSlideOffsetY = 28f,
            bool firstItemSlidesFromAbove = true)
        {
            return new UIStaggerConfig
            {
                style = UIStaggerStyle.SlideUp,
                stagger = stagger,
                duration = duration,
                slideOffsetY = slideOffsetY,
                firstItemSlideOffsetY = firstItemSlideOffsetY,
                firstItemSlidesFromAbove = firstItemSlidesFromAbove,
                startScale = startScale,
                startRotationZ = 0f
            };
        }

        public static UIStaggerConfig FadeScale(float stagger = 0.08f, float duration = 0.38f, float startScale = 0.9f)
        {
            return new UIStaggerConfig
            {
                style = UIStaggerStyle.FadeScale,
                stagger = stagger,
                duration = duration,
                slideOffsetY = 0f,
                startScale = startScale,
                startRotationZ = 0f
            };
        }
    }

    public static class UIDominoTween
    {
        private struct CachedState
        {
            public Vector3 scale;
            public Vector3 rotation;
            public Vector2 anchoredPosition;
            public float alpha;
        }

        private static readonly Dictionary<RectTransform, CachedState> CachedStates = new();

        public static void Play(
            IReadOnlyList<RectTransform> targets,
            float stagger = 0.08f,
            float duration = 0.4f,
            float startRotationZ = 18f,
            float startScale = 0f)
        {
            Play(targets, UIStaggerConfig.Domino(stagger, duration, startRotationZ));
        }

        public static void PrepareHidden(IReadOnlyList<RectTransform> targets, UIStaggerConfig config)
        {
            if (targets == null || targets.Count == 0)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                RectTransform target = targets[i];
                if (target == null)
                    continue;

                DOTween.Kill(target);
                EnsureCachedState(target);
                ApplyHiddenInitialState(target, config, i);
            }
        }

        public static void Play(IReadOnlyList<RectTransform> targets, UIStaggerConfig config)
        {
            if (targets == null || targets.Count == 0)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                RectTransform target = targets[i];
                if (target == null)
                    continue;

                DOTween.Kill(target);
                EnsureCachedState(target);

                CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
                CachedState cached = CachedStates[target];
                ApplyHiddenInitialState(target, config, i);

                float delay = i * config.stagger;

                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(target);
                sequence.SetDelay(delay);

                switch (config.style)
                {
                    case UIStaggerStyle.SlideUp:
                        AppendSlideUpTweens(target, canvasGroup, cached, config, sequence);
                        break;
                    case UIStaggerStyle.FadeScale:
                        AppendFadeScaleTweens(target, canvasGroup, cached, config, sequence);
                        break;
                    default:
                        AppendDominoTweens(target, canvasGroup, cached, config, sequence);
                        break;
                }
            }
        }

        public static void PlayClose(
            IReadOnlyList<RectTransform> targets,
            float stagger,
            float duration,
            float endScaleMultiplier,
            Action onComplete = null)
        {
            if (targets == null || targets.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int remaining = targets.Count;

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                RectTransform target = targets[i];
                if (target == null)
                {
                    remaining--;
                    continue;
                }

                DOTween.Kill(target);
                EnsureCachedState(target);

                CachedState cached = CachedStates[target];
                CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
                float delay = (targets.Count - 1 - i) * stagger;

                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(target);
                sequence.SetDelay(delay);
                sequence.Join(target.DOScale(cached.scale * endScaleMultiplier, duration).SetEase(Ease.InBack));
                sequence.Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad));
                sequence.OnComplete(() =>
                {
                    DOTween.Kill(target);
                    remaining--;
                    if (remaining <= 0)
                        onComplete?.Invoke();
                });
            }

            if (remaining <= 0)
                onComplete?.Invoke();
        }

        public static void StopTweens(IReadOnlyList<RectTransform> targets)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    DOTween.Kill(targets[i]);
            }
        }

        public static void Kill(IReadOnlyList<RectTransform> targets)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    Kill(targets[i]);
            }
        }

        public static void Kill(RectTransform target)
        {
            if (target == null)
                return;

            DOTween.Kill(target);

            if (!CachedStates.TryGetValue(target, out CachedState cached))
                return;

            target.localScale = cached.scale;
            target.localRotation = Quaternion.Euler(cached.rotation);
            target.anchoredPosition = cached.anchoredPosition;

            if (target.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup.alpha = cached.alpha;

            CachedStates.Remove(target);
        }

        public static List<RectTransform> CollectActiveChildren(Transform contentRoot)
        {
            List<RectTransform> result = new();
            if (contentRoot == null)
                return result;

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                    continue;

                if (child is RectTransform rect)
                    result.Add(rect);
            }

            return result;
        }

        public static List<RectTransform> CollectStoreCards(Transform panelRoot)
        {
            Transform content = FindScrollContent(panelRoot);
            List<RectTransform> cards = CollectActiveChildren(content);

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                RectTransform card = cards[i];
                if (card == null)
                {
                    cards.RemoveAt(i);
                    continue;
                }

                if (!card.TryGetComponent<Button>(out _))
                    cards.RemoveAt(i);
            }

            return cards;
        }

        public static Transform FindScrollContent(Transform panelRoot)
        {
            if (panelRoot == null)
                return null;

            Transform scrollView = panelRoot.Find("Scroll View");
            if (scrollView == null)
                return null;

            Transform viewportContent = scrollView.Find("Viewport/Content");
            if (viewportContent != null)
                return viewportContent;

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            return scrollRect != null ? scrollRect.content : null;
        }

        private static void AppendDominoTweens(
            RectTransform target,
            CanvasGroup canvasGroup,
            CachedState cached,
            UIStaggerConfig config,
            Sequence sequence)
        {
            sequence.Append(target.DOScale(cached.scale, config.duration).SetEase(Ease.OutBack));
            sequence.Join(target.DOLocalRotate(cached.rotation, config.duration).SetEase(Ease.OutCubic));
            sequence.Join(canvasGroup.DOFade(cached.alpha, config.duration * 0.85f).SetEase(Ease.OutQuad));
        }

        private static void AppendSlideUpTweens(
            RectTransform target,
            CanvasGroup canvasGroup,
            CachedState cached,
            UIStaggerConfig config,
            Sequence sequence)
        {
            sequence.Append(target.DOAnchorPos(cached.anchoredPosition, config.duration).SetEase(Ease.OutCubic));
            sequence.Join(target.DOScale(cached.scale, config.duration).SetEase(Ease.OutCubic));
            sequence.Join(canvasGroup.DOFade(cached.alpha, config.duration * 0.9f).SetEase(Ease.OutQuad));
        }

        private static void AppendFadeScaleTweens(
            RectTransform target,
            CanvasGroup canvasGroup,
            CachedState cached,
            UIStaggerConfig config,
            Sequence sequence)
        {
            sequence.Append(target.DOScale(cached.scale, config.duration).SetEase(Ease.OutBack, 1.05f));
            sequence.Join(canvasGroup.DOFade(cached.alpha, config.duration * 0.9f).SetEase(Ease.OutQuad));
        }

        private static void ApplyHiddenInitialState(RectTransform target, UIStaggerConfig config, int index)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
            canvasGroup.alpha = 0f;

            CachedState cached = CachedStates[target];

            if (config.style == UIStaggerStyle.FadeScale)
            {
                target.localScale = cached.scale * config.startScale;
                return;
            }

            if (config.style == UIStaggerStyle.SlideUp)
            {
                bool fromAbove = index == 0 && config.firstItemSlidesFromAbove;
                float offset = index == 0 && config.firstItemSlideOffsetY > 0f
                    ? config.firstItemSlideOffsetY
                    : config.slideOffsetY;

                target.localScale = cached.scale * config.startScale;
                target.anchoredPosition = cached.anchoredPosition + new Vector2(0f, fromAbove ? offset : -offset);
                return;
            }

            target.localScale = Vector3.one * config.startScale;

            Vector3 euler = cached.rotation;
            target.localRotation = Quaternion.Euler(euler.x, euler.y, config.startRotationZ);
        }

        private static void EnsureCachedState(RectTransform target)
        {
            if (CachedStates.ContainsKey(target))
                return;

            CacheState(target);
        }

        private static void CacheState(RectTransform target)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            CachedStates[target] = new CachedState
            {
                scale = target.localScale,
                rotation = target.localEulerAngles,
                anchoredPosition = target.anchoredPosition,
                alpha = canvasGroup != null ? canvasGroup.alpha : 1f
            };
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            if (!target.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

            return canvasGroup;
        }
    }
}

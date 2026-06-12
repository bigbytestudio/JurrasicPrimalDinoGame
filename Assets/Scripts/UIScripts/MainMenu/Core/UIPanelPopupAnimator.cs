using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class UIPanelPopupAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private float openDuration = 0.35f;
        [SerializeField] private float closeDuration = 0.25f;
        [SerializeField] private float openScaleFrom = 0.88f;
        [SerializeField] private float overlayAlpha = 0.95f;

        private Vector3 shownScale = Vector3.one;
        private Tween activeTween;
        private bool isInitialized;

        private void Awake()
        {
            TryAutoBind();
            CacheShownState();
        }

        private void OnDisable()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        public void PlayOpen(Action onComplete = null)
        {
            TryAutoBind();
            CacheShownState();
            KillActiveTween();

            if (popupRoot != null)
            {
                popupRoot.localScale = shownScale * openScaleFrom;
                popupRoot.gameObject.SetActive(true);
            }

            SetOverlayAlpha(0f);

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);

            if (overlayGroup != null)
                sequence.Join(overlayGroup.DOFade(overlayAlpha, openDuration).SetEase(Ease.OutQuad));
            else if (overlayImage != null)
                sequence.Join(overlayImage.DOFade(overlayAlpha, openDuration).SetEase(Ease.OutQuad));

            if (popupRoot != null)
                sequence.Join(popupRoot.DOScale(shownScale, openDuration).SetEase(Ease.OutBack));

            activeTween = sequence.OnComplete(() =>
            {
                activeTween = null;
                onComplete?.Invoke();
            });
        }

        public void PlayClose(Action onComplete)
        {
            TryAutoBind();
            KillActiveTween();

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(this);

            if (popupRoot != null)
                sequence.Join(popupRoot.DOScale(shownScale * openScaleFrom, closeDuration).SetEase(Ease.InBack));

            if (overlayGroup != null)
                sequence.Join(overlayGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
            else if (overlayImage != null)
                sequence.Join(overlayImage.DOFade(0f, closeDuration).SetEase(Ease.InQuad));

            activeTween = sequence.OnComplete(() =>
            {
                activeTween = null;
                onComplete?.Invoke();
            });
        }

        private void TryAutoBind()
        {
            if (popupRoot == null)
            {
                Transform popup = transform.Find("Popup");
                if (popup != null)
                    popupRoot = popup as RectTransform;
            }

            if (overlayGroup == null && overlayImage == null)
            {
                Transform overlay = transform.Find("BlackOverlay");
                if (overlay != null)
                {
                    overlayGroup = overlay.GetComponent<CanvasGroup>();
                    overlayImage = overlay.GetComponent<Image>();
                }
            }
        }

        private void CacheShownState()
        {
            if (isInitialized || popupRoot == null)
                return;

            shownScale = popupRoot.localScale;
            isInitialized = true;
        }

        private void KillActiveTween()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = alpha;
                return;
            }

            if (overlayImage == null)
                return;

            Color color = overlayImage.color;
            color.a = alpha;
            overlayImage.color = color;
        }
    }
}

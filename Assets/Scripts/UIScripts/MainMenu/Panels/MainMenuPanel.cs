using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Root main-menu hub. Routes button clicks to panels or lightweight actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuPanel : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button profileButton;
        [SerializeField] private Button freeRewardButton;
        [SerializeField] private Button moreGamesButton;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button rateUsButton;
        [SerializeField] private Button dailyMissionButton;
        [SerializeField] private Button dinoSelectionButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button discordButton;
        [SerializeField] private Button startButton;

        [Header("Profile Summary")]
        [SerializeField] private MainMenuProfileView profileView;
        [SerializeField] private MainMenuSelectedCreatureView selectedCreatureView;

        [Header("Open / Close Animation")]
        [SerializeField] private RectTransform menuPopupRoot;
        [SerializeField] private bool playMenuAnimation = true;
        [SerializeField] private UIStaggerStyle revealStyle = UIStaggerStyle.SlideUp;
        [SerializeField] private float revealStagger = 0.06f;
        [SerializeField] private float revealDuration = 0.42f;
        [SerializeField] private float revealSlideOffset = 36f;
        [SerializeField] private float revealStartScale = 0.94f;
        [SerializeField] private float closeDuration = 0.24f;
        [SerializeField] private float closeSlideOffset = 28f;

        private readonly List<RectTransform> animatedMenuItems = new();

        private MenuManager menuManager;
        private bool isHiding;

        public void Initialize(MenuManager manager)
        {
            menuManager = manager;
            EnsureDependencies();
            BindButtons();
        }

        public void Show()
        {
            isHiding = false;

            gameObject.SetActive(true);
            profileView?.Refresh();
            selectedCreatureView?.Refresh();
            PlayOpenAnimation();
        }

        public void Hide()
        {
            Hide(null);
        }

        public void Hide(Action onComplete)
        {
            if (!gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return;
            }

            if (isHiding)
            {
                onComplete?.Invoke();
                return;
            }

            if (!playMenuAnimation || animatedMenuItems.Count == 0)
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            isHiding = true;
            PlayCloseAnimation(() =>
            {
                isHiding = false;
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        private void EnsureDependencies()
        {
            profileView ??= GetComponent<MainMenuProfileView>();
            if (profileView == null)
                profileView = gameObject.AddComponent<MainMenuProfileView>();

            selectedCreatureView ??= GetComponent<MainMenuSelectedCreatureView>();
            if (selectedCreatureView == null)
                selectedCreatureView = gameObject.AddComponent<MainMenuSelectedCreatureView>();

            if (menuPopupRoot == null)
            {
                Transform popup = transform.Find("MainPopup");
                if (popup is RectTransform popupRect)
                    menuPopupRoot = popupRect;
            }
        }

        private void BindButtons()
        {
            Bind(profileButton, () => menuManager.OpenPanel(MenuPanelId.Profile));
            Bind(freeRewardButton, () => menuManager.OpenPanel(MenuPanelId.FreeReward));
            Bind(moreGamesButton, () => menuManager.OpenMoreGames());
            Bind(privacyPolicyButton, () => menuManager.OpenPrivacyPolicy());
            Bind(rateUsButton, () => menuManager.OpenRateUs());
            Bind(dailyMissionButton, () => menuManager.OpenPanel(MenuPanelId.DailyMission));
            Bind(dinoSelectionButton, () => menuManager.OpenPanel(MenuPanelId.DinoSelection));
            Bind(storeButton, () => menuManager.OpenPanel(MenuPanelId.Store));
            Bind(settingsButton, () => menuManager.OpenPanel(MenuPanelId.Settings));
            Bind(discordButton, () => menuManager.OpenDiscord());
            Bind(startButton, () => menuManager.StartGame());
        }

        private void PlayOpenAnimation()
        {
            CollectAnimatedMenuItems();

            if (!playMenuAnimation || animatedMenuItems.Count == 0)
                return;

            StopMenuAnimation();
            UIDominoTween.PrepareHidden(animatedMenuItems, BuildRevealConfig());
            UIDominoTween.Play(animatedMenuItems, BuildRevealConfig());
        }

        private void PlayCloseAnimation(Action onComplete)
        {
            StopMenuAnimation();

            if (animatedMenuItems.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int remaining = animatedMenuItems.Count;
            for (int i = animatedMenuItems.Count - 1; i >= 0; i--)
            {
                RectTransform target = animatedMenuItems[i];
                if (target == null)
                {
                    remaining--;
                    continue;
                }

                DOTween.Kill(target);
                CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

                float delay = (animatedMenuItems.Count - 1 - i) * (revealStagger * 0.65f);
                Vector2 startPosition = target.anchoredPosition;
                Vector3 startScale = target.localScale;
                float startAlpha = canvasGroup.alpha;

                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(target);
                sequence.SetDelay(delay);
                sequence.Join(target.DOScale(startScale * revealStartScale, closeDuration).SetEase(Ease.InBack));
                sequence.Join(target.DOAnchorPosY(startPosition.y - closeSlideOffset, closeDuration).SetEase(Ease.InQuad));
                sequence.Join(canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
                sequence.OnComplete(() =>
                {
                    target.localScale = startScale;
                    target.anchoredPosition = startPosition;
                    canvasGroup.alpha = startAlpha;

                    remaining--;
                    if (remaining <= 0)
                        onComplete?.Invoke();
                });
            }

            if (remaining <= 0)
                onComplete?.Invoke();
        }

        private void CollectAnimatedMenuItems()
        {
            animatedMenuItems.Clear();

            if (menuPopupRoot == null)
                return;

            animatedMenuItems.AddRange(UIDominoTween.CollectActiveChildren(menuPopupRoot));
        }

        private UIStaggerConfig BuildRevealConfig()
        {
            if (revealStyle == UIStaggerStyle.SlideUp)
            {
                return UIStaggerConfig.SlideUp(
                    revealStagger,
                    revealDuration,
                    revealSlideOffset,
                    revealStartScale);
            }

            return UIStaggerConfig.Domino(revealStagger, revealDuration);
        }

        private void StopMenuAnimation()
        {
            UIDominoTween.Kill(animatedMenuItems);
        }

        private void OnDisable()
        {
            StopMenuAnimation();
            isHiding = false;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}

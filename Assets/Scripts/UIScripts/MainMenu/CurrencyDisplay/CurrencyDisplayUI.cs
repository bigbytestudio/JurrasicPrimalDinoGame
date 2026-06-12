using DG.Tweening;
using SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Single shared currency bar shown across all menu panels.
    /// Reads DNA and bones from <see cref="GameDataSave"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrencyDisplayUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text dnaAmountText;
        [SerializeField] private TMP_Text bonesAmountText;
        [SerializeField] private Button dnaButton;
        [SerializeField] private Button bonesButton;

        [Header("Show / Hide")]
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.22f;
        [SerializeField] private float slideOffset = 28f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 shownPosition;
        private Tween activeTween;
        private bool isVisible = true;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (rectTransform != null)
                shownPosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            EnsureGameDataLoaded();
            GameDataSave.CurrencyChanged += Refresh;
            BindButtons();
            Refresh();
        }

        private static void EnsureGameDataLoaded()
        {
            if (GameDataSave.Instance != null)
                return;

            GameDataSave gameData = GameDataSave.Load();
            GameDataSave.Bind(gameData);
            SaveDataService.Instance.Register(gameData);
        }

        private void OnDisable()
        {
            GameDataSave.CurrencyChanged -= Refresh;
            activeTween?.Kill();
            activeTween = null;
        }

        public void SetVisible(bool visible, bool immediate = false)
        {
            if (rectTransform == null)
            {
                gameObject.SetActive(visible);
                isVisible = visible;
                return;
            }

            if (visible == isVisible && gameObject.activeSelf == visible && activeTween == null && !immediate)
                return;

            activeTween?.Kill();
            activeTween = null;

            if (immediate)
            {
                ApplyImmediate(visible);
                return;
            }

            if (visible)
                PlayShow();
            else
                PlayHide();
        }

        private void PlayShow()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = shownPosition + new Vector2(0f, slideOffset);

            activeTween = DOTween.Sequence()
                .Join(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad))
                .Join(rectTransform.DOAnchorPos(shownPosition, showDuration).SetEase(Ease.OutCubic))
                .OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    activeTween = null;
                });

            isVisible = true;
        }

        private void PlayHide()
        {
            if (!gameObject.activeInHierarchy)
            {
                isVisible = false;
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            activeTween = DOTween.Sequence()
                .Join(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad))
                .Join(rectTransform.DOAnchorPos(shownPosition + new Vector2(0f, slideOffset), hideDuration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    rectTransform.anchoredPosition = shownPosition;
                    canvasGroup.alpha = 1f;
                    activeTween = null;
                });

            isVisible = false;
        }

        private void ApplyImmediate(bool visible)
        {
            gameObject.SetActive(visible);
            rectTransform.anchoredPosition = shownPosition;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            isVisible = visible;
        }

        private void BindButtons()
        {
            if (dnaButton != null)
            {
                dnaButton.onClick.RemoveAllListeners();
                dnaButton.onClick.AddListener(OpenDnaStore);
            }

            if (bonesButton != null)
            {
                bonesButton.onClick.RemoveAllListeners();
                bonesButton.onClick.AddListener(OpenBonesStore);
            }
        }

        public void Refresh()
        {
            GameDataSave data = GameDataSave.Instance;
            if (data == null)
                return;

            if (dnaAmountText != null)
                dnaAmountText.text = data.dnaCurrency.ToString();

            if (bonesAmountText != null)
                bonesAmountText.text = data.bonesCurrency.ToString();
        }

        private static void OpenDnaStore()
        {
            MenuManager.Instance?.OpenPanel(
                MenuPanelId.Store,
                MenuContext.ForStore(StoreTab.Dna, toggleCloseWhenSameTab: false));
        }

        private static void OpenBonesStore()
        {
            MenuManager.Instance?.OpenPanel(
                MenuPanelId.Store,
                MenuContext.ForStore(StoreTab.Bones, toggleCloseWhenSameTab: false));
        }
    }
}

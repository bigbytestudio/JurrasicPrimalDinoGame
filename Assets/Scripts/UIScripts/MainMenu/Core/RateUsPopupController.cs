using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class RateUsPopupController : MonoBehaviour
    {
        private const string PopupObjectName = "RateUS";

        [SerializeField] private Button laterButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button rateUsButton;
        [SerializeField] private UIPanelPopupAnimator popupAnimator;

        private bool isClosing;
        private bool isInitialized;

        public static RateUsPopupController Instance { get; private set; }

        public static void Show()
        {
            RateUsPopupController popup = ResolveInstance();
            if (popup == null)
            {
                Debug.LogWarning("Rate Us popup is not available in the scene.");
                return;
            }

            popup.Present();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            UnwireButtons();

            if (Instance == this)
                Instance = null;
        }

        private void Present()
        {
            EnsureInitialized();
            isClosing = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            popupAnimator?.PlayOpen();
        }

        private void HandleLater()
        {
            if (isClosing)
                return;

            CloseAnimated();
        }

        private void HandleRateUs()
        {
            if (isClosing)
                return;

            MenuManager.Instance?.OpenRateUs();
            CloseAnimated();
        }

        private void CloseAnimated()
        {
            isClosing = true;

            if (popupAnimator != null)
            {
                popupAnimator.PlayClose(() =>
                {
                    gameObject.SetActive(false);
                    isClosing = false;
                });
                return;
            }

            gameObject.SetActive(false);
            isClosing = false;
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            TryAutoBind();
            popupAnimator ??= GetComponent<UIPanelPopupAnimator>() ?? gameObject.AddComponent<UIPanelPopupAnimator>();
            WireButtons();
            isInitialized = true;
        }

        private void WireButtons()
        {
            if (laterButton != null)
                laterButton.onClick.AddListener(HandleLater);

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleLater);

            if (rateUsButton != null)
                rateUsButton.onClick.AddListener(HandleRateUs);
        }

        private void UnwireButtons()
        {
            if (laterButton != null)
                laterButton.onClick.RemoveListener(HandleLater);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleLater);

            if (rateUsButton != null)
                rateUsButton.onClick.RemoveListener(HandleRateUs);
        }

        private void TryAutoBind()
        {
            Transform popup = transform.Find("Popup");
            if (popup == null)
                return;

            laterButton ??= FindButton(popup, "NoBtn");
            rateUsButton ??= FindButton(popup, "yes");
        }

        private static Button FindButton(Transform root, string objectName)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != objectName)
                    continue;

                Button button = children[i].GetComponent<Button>();
                if (button != null)
                    return button;
            }

            return null;
        }

        private static RateUsPopupController ResolveInstance()
        {
            if (Instance != null)
                return Instance;

            RateUsPopupController existing = FindObjectOfType<RateUsPopupController>(true);
            if (existing != null)
                return existing;

            GameObject popupObject = GameObject.Find(PopupObjectName);
            if (popupObject == null)
                return null;

            return popupObject.AddComponent<RateUsPopupController>();
        }
    }
}

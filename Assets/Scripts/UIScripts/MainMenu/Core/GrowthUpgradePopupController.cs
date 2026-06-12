using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class GrowthUpgradePopupController : MonoBehaviour
    {
        private const string ConfirmationPopupName = "UpgradeDinoPopup";
        private const string InsufficientDnaPopupName = "InsufficientDNApopUP";

        [SerializeField] private GameObject confirmationPopup;
        [SerializeField] private GameObject insufficientDnaPopup;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Button insufficientQuitButton;
        [SerializeField] private Button insufficientTryAgainButton;
        [SerializeField] private TMP_Text upgradeCostText;

        private CreatureProfile pendingProfile;
        private Action onUpgraded;

        public static GrowthUpgradePopupController Instance { get; private set; }

        public static void ShowConfirmation(CreatureProfile profile, Action upgradedCallback = null)
        {
            if (profile == null)
                return;

            GrowthUpgradePopupController controller = ResolveInstance();
            if (controller == null)
            {
                Debug.LogWarning("Growth upgrade popup controller is not available in the scene.", profile);
                return;
            }

            controller.PresentConfirmation(profile, upgradedCallback);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            TryAutoBind();
            WireButtons();
            HideAll();
        }

        private void OnDestroy()
        {
            UnwireButtons();

            if (Instance == this)
                Instance = null;
        }

        private void PresentConfirmation(CreatureProfile profile, Action upgradedCallback)
        {
            pendingProfile = profile;
            onUpgraded = upgradedCallback;

            if (upgradeCostText != null)
                upgradeCostText.text = $"{GrowthUpgradeUtility.GetUpgradeCost(profile)} DNA";

            HideInsufficientDna();
            if (confirmationPopup != null)
                confirmationPopup.SetActive(true);
        }

        private void HandleYesClicked()
        {
            if (pendingProfile == null)
            {
                HideConfirmation();
                return;
            }

            GrowthUpgradeResult result = GrowthUpgradeUtility.TryUpgrade(pendingProfile);
            switch (result)
            {
                case GrowthUpgradeResult.Success:
                    HideConfirmation();
                    onUpgraded?.Invoke();
                    break;

                case GrowthUpgradeResult.InsufficientDna:
                    HideConfirmation();
                    ShowInsufficientDna();
                    break;

                default:
                    HideConfirmation();
                    break;
            }
        }

        private void HandleNoClicked()
        {
            HideConfirmation();
        }

        private void HandleInsufficientDismiss()
        {
            HideInsufficientDna();
        }

        private void HideConfirmation()
        {
            pendingProfile = null;
            onUpgraded = null;

            if (confirmationPopup != null)
                confirmationPopup.SetActive(false);
        }

        private void ShowInsufficientDna()
        {
            if (insufficientDnaPopup != null)
                insufficientDnaPopup.SetActive(true);
        }

        private void HideInsufficientDna()
        {
            if (insufficientDnaPopup != null)
                insufficientDnaPopup.SetActive(false);
        }

        private void HideAll()
        {
            HideConfirmation();
            HideInsufficientDna();
        }

        private void WireButtons()
        {
            if (yesButton != null)
                yesButton.onClick.AddListener(HandleYesClicked);

            if (noButton != null)
                noButton.onClick.AddListener(HandleNoClicked);

            if (insufficientQuitButton != null)
                insufficientQuitButton.onClick.AddListener(HandleInsufficientDismiss);

            if (insufficientTryAgainButton != null)
                insufficientTryAgainButton.onClick.AddListener(HandleInsufficientDismiss);
        }

        private void UnwireButtons()
        {
            if (yesButton != null)
                yesButton.onClick.RemoveListener(HandleYesClicked);

            if (noButton != null)
                noButton.onClick.RemoveListener(HandleNoClicked);

            if (insufficientQuitButton != null)
                insufficientQuitButton.onClick.RemoveListener(HandleInsufficientDismiss);

            if (insufficientTryAgainButton != null)
                insufficientTryAgainButton.onClick.RemoveListener(HandleInsufficientDismiss);
        }

        private void TryAutoBind()
        {
            if (confirmationPopup == null)
                confirmationPopup = FindChildPopup(ConfirmationPopupName);

            if (insufficientDnaPopup == null)
                insufficientDnaPopup = FindChildPopup(InsufficientDnaPopupName);

            if (confirmationPopup != null)
            {
                if (yesButton == null)
                    yesButton = FindButton(confirmationPopup.transform, "yes");

                if (noButton == null)
                    noButton = FindButton(confirmationPopup.transform, "NoBtn");

                if (upgradeCostText == null && yesButton != null)
                    upgradeCostText = yesButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (insufficientDnaPopup != null)
            {
                if (insufficientQuitButton == null)
                    insufficientQuitButton = FindButton(insufficientDnaPopup.transform, "quitBtn");

                if (insufficientTryAgainButton == null)
                    insufficientTryAgainButton = FindButton(insufficientDnaPopup.transform, "tryAgainBtn");
            }
        }

        private GameObject FindChildPopup(string popupName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == popupName)
                    return children[i].gameObject;
            }

            return null;
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

        private static GrowthUpgradePopupController ResolveInstance()
        {
            if (Instance != null)
                return Instance;

            GrowthUpgradePopupController existing = FindObjectOfType<GrowthUpgradePopupController>(true);
            if (existing != null)
                return existing;

            MenuManager menuManager = MenuManager.Instance;
            if (menuManager == null)
                menuManager = FindObjectOfType<MenuManager>();

            GameObject panelRootObject = GameObject.Find("PanelRoot");
            Transform panelRoot = panelRootObject != null ? panelRootObject.transform : null;

            if (panelRoot == null)
                return null;

            return panelRoot.GetComponent<GrowthUpgradePopupController>()
                ?? panelRoot.gameObject.AddComponent<GrowthUpgradePopupController>();
        }
    }
}

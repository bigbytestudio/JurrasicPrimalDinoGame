using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class ProfileEditNamePopup : MonoBehaviour
    {
        private const string PopupObjectName = "EditNamePopup";

        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private UIPanelPopupAnimator popupAnimator;
        [SerializeField] private int maxNameLength = 20;

        private bool isClosing;
        private bool isInitialized;

        public static ProfileEditNamePopup Instance { get; private set; }

        public static void Show()
        {
            ProfileEditNamePopup popup = ResolveInstance();
            if (popup == null)
            {
                Debug.LogWarning("Edit Name popup is not available in the scene.");
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

            string currentName = GameDataSave.Instance != null ? GameDataSave.Instance.playerName : "Player";
            if (nameInput != null)
            {
                nameInput.characterLimit = maxNameLength;
                nameInput.text = currentName;
            }

            popupAnimator?.PlayOpen(() => nameInput?.ActivateInputField());
        }

        private void HandleConfirm()
        {
            if (isClosing)
                return;

            string newName = nameInput != null ? nameInput.text : string.Empty;
            GameDataSave.Instance?.SetPlayerName(newName);
            CloseAnimated();
        }

        private void HandleCancel()
        {
            if (isClosing)
                return;

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
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancel);
        }

        private void UnwireButtons()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirm);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancel);
        }

        private void TryAutoBind()
        {
            Transform popup = transform.Find("Popup");
            if (popup == null)
                return;

            nameInput ??= popup.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();
            confirmButton ??= FindButton(popup, "yes");
            cancelButton ??= FindButton(popup, "NoBtn (1)");
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

        private static ProfileEditNamePopup ResolveInstance()
        {
            if (Instance != null)
                return Instance;

            ProfileEditNamePopup existing = FindObjectOfType<ProfileEditNamePopup>(true);
            if (existing != null)
                return existing;

            GameObject popupObject = GameObject.Find(PopupObjectName);
            if (popupObject == null)
                return null;

            return popupObject.AddComponent<ProfileEditNamePopup>();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class ProfilePanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button editNameButton;
        [SerializeField] private ProfilePanelView panelView;
        [SerializeField] private UIPanelPopupAnimator popupAnimator;

        private bool isClosing;

        public override MenuPanelId PanelId => MenuPanelId.Profile;

        private void Awake()
        {
            EnsurePanelDependencies();

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseRequested);

            if (editNameButton == null)
            {
                Transform editNameTransform = transform.Find("Popup/nameBox/EditName");
                if (editNameTransform != null)
                    editNameButton = editNameTransform.GetComponent<Button>();
            }

            if (editNameButton != null)
                editNameButton.onClick.AddListener(HandleEditNameRequested);
        }

        private void EnsurePanelDependencies()
        {
            panelView ??= GetComponent<ProfilePanelView>();
            if (panelView == null)
                panelView = gameObject.AddComponent<ProfilePanelView>();

            popupAnimator ??= GetComponent<UIPanelPopupAnimator>();
            if (popupAnimator == null)
                popupAnimator = gameObject.AddComponent<UIPanelPopupAnimator>();
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            isClosing = false;
            panelView?.Refresh();
            popupAnimator?.PlayOpen();
        }

        private void HandleEditNameRequested()
        {
            ProfileEditNamePopup.Show();
        }

        private void HandleCloseRequested()
        {
            if (isClosing)
                return;

            isClosing = true;

            if (popupAnimator != null)
                popupAnimator.PlayClose(CloseSelf);
            else
                CloseSelf();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class ProfilePanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;

        public override MenuPanelId PanelId => MenuPanelId.Profile;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            // Load profile data here when backend is ready.
        }
    }
}

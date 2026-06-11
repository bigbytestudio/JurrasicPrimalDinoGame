using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class SettingsPanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;

        public override MenuPanelId PanelId => MenuPanelId.Settings;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }
    }
}

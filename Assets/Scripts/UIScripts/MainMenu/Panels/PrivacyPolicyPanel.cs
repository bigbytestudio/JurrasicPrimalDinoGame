using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Optional in-app privacy policy panel. Main menu can also open an external URL.
    /// </summary>
    public sealed class PrivacyPolicyPanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;

        public override MenuPanelId PanelId => MenuPanelId.PrivacyPolicy;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }
    }
}

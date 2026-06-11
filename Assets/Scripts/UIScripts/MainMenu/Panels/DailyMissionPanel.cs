using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class DailyMissionPanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Text statusText;

        public override MenuPanelId PanelId => MenuPanelId.DailyMission;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);

            if (statusText != null)
                statusText.text = "Daily mission bonus is coming soon.";
        }
    }
}

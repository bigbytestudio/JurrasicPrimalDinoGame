using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class FreeRewardPanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button claimButton;

        public override MenuPanelId PanelId => MenuPanelId.FreeReward;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);

            if (claimButton != null)
                claimButton.onClick.AddListener(ClaimReward);
        }

        private void ClaimReward()
        {
            // Hook ad / reward logic here.
        }
    }
}

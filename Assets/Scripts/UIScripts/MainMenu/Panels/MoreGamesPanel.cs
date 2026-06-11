using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Optional in-app panel if you prefer listing games instead of opening a URL.
    /// Main menu currently uses MenuManager.OpenMoreGames() directly.
    /// </summary>
    public sealed class MoreGamesPanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;

        public override MenuPanelId PanelId => MenuPanelId.MoreGames;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }
    }
}

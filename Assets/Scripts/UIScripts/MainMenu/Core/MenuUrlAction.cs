using UnityEngine;

namespace DinoGame.UI.Menu
{
    public sealed class MenuUrlAction : IMenuAction
    {
        private readonly string url;

        public MenuUrlAction(string url)
        {
            this.url = url;
        }

        public void Execute(MenuManager menuManager)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("MenuUrlAction received an empty URL.");
                return;
            }

            Application.OpenURL(url);
        }
    }
}

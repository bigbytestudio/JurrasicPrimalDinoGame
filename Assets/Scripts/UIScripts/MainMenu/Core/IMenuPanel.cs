namespace DinoGame.UI.Menu
{
    public interface IMenuPanel
    {
        MenuPanelId PanelId { get; }
        bool DestroyOnClose { get; }

        void OnPanelOpened(MenuContext context);
        void OnPanelClosed();
    }
}

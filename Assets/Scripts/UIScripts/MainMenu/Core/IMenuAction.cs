namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Lightweight menu actions that do not require spawning a panel (e.g. open URL, rate app).
    /// </summary>
    public interface IMenuAction
    {
        void Execute(MenuManager menuManager);
    }
}

namespace DinoGame.UI.Menu
{
    public enum StoreTab
    {
        General = 0,
        Dna = 1,
        Bones = 2
    }

    public sealed class MenuContext
    {
        public static readonly MenuContext Empty = new();

        public StoreTab StoreTab { get; private set; } = StoreTab.General;

        public static MenuContext ForStore(StoreTab tab)
        {
            return new MenuContext { StoreTab = tab };
        }
    }
}

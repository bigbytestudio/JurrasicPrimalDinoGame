namespace DinoGame.UI.Menu
{
    public enum StoreTab
    {
        Bones = 0,
        Dna = 1,
        Offer = 2,
        Free = 3
    }

    public sealed class MenuContext
    {
        public static readonly MenuContext Empty = new();

        public StoreTab StoreTab { get; private set; } = StoreTab.Bones;
        public bool ToggleCloseWhenSameTab { get; private set; } = true;

        public static MenuContext ForStore(StoreTab tab, bool toggleCloseWhenSameTab = true)
        {
            return new MenuContext
            {
                StoreTab = tab,
                ToggleCloseWhenSameTab = toggleCloseWhenSameTab
            };
        }
    }
}

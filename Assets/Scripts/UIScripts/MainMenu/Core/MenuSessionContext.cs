namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Lightweight session flags that survive scene loads within the same app run.
    /// </summary>
    public static class MenuSessionContext
    {
        private static bool leftForGameplay;

        public static void MarkLeftForGameplay()
        {
            leftForGameplay = true;
        }

        public static bool TryConsumeReturnFromGameplay()
        {
            if (!leftForGameplay)
                return false;

            leftForGameplay = false;
            return true;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Root main-menu hub. Routes button clicks to panels or lightweight actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuPanel : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button profileButton;
        [SerializeField] private Button freeRewardButton;
        [SerializeField] private Button moreGamesButton;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button rateUsButton;
        [SerializeField] private Button dailyMissionButton;
        [SerializeField] private Button dinoSelectionButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button discordButton;
        [SerializeField] private Button startButton;

        private MenuManager menuManager;

        public void Initialize(MenuManager manager)
        {
            menuManager = manager;
            BindButtons();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void BindButtons()
        {
            Bind(profileButton, () => menuManager.OpenPanel(MenuPanelId.Profile));
            Bind(freeRewardButton, () => menuManager.OpenPanel(MenuPanelId.FreeReward));
            Bind(moreGamesButton, () => menuManager.OpenMoreGames());
            Bind(privacyPolicyButton, () => menuManager.OpenPrivacyPolicy());
            Bind(rateUsButton, () => menuManager.OpenRateUs());
            Bind(dailyMissionButton, () => menuManager.OpenPanel(MenuPanelId.DailyMission));
            Bind(dinoSelectionButton, () => menuManager.OpenPanel(MenuPanelId.DinoSelection));
            Bind(storeButton, () => menuManager.OpenPanel(MenuPanelId.Store));
            Bind(settingsButton, () => menuManager.OpenPanel(MenuPanelId.Settings));
            Bind(discordButton, () => menuManager.OpenDiscord());
            Bind(startButton, () => menuManager.StartGame());
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}

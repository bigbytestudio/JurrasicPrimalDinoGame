using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Central menu orchestrator. Spawns overlay panels on demand and destroys them to save memory.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "GamePlay";

        [Header("UI Roots")]
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private Transform panelRoot;
        [SerializeField] private MenuPanelRegistry panelRegistry;

        [Header("External Links")]
        [SerializeField] private string moreGamesUrl;
        [SerializeField] private string privacyPolicyUrl;
        [SerializeField] private string rateUsUrl;
        [SerializeField] private string discordUrl;

        private readonly Dictionary<MenuPanelId, UIPanelBase> cachedPanels = new();
        private UIPanelBase activeOverlay;

        public MainMenuPanel MainMenu => mainMenuPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (panelRoot == null)
                panelRoot = transform;

            mainMenuPanel?.Initialize(this);
        }

        private void Start()
        {
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ShowMainMenu()
        {
            CloseCurrentPanel();
            mainMenuPanel?.Show();
        }

        public void OpenPanel(MenuPanelId panelId, MenuContext context = null)
        {
            if (panelId == MenuPanelId.None || panelId == MenuPanelId.MainMenu)
            {
                ShowMainMenu();
                return;
            }

            if (panelRegistry == null)
            {
                Debug.LogError("MenuManager is missing a MenuPanelRegistry.", this);
                return;
            }

            if (!panelRegistry.TryGetEntry(panelId, out MenuPanelRegistry.PanelEntry entry))
            {
                Debug.LogError($"No menu panel prefab registered for '{panelId}'.", this);
                return;
            }

            CloseCurrentPanel();
            mainMenuPanel?.Hide();

            activeOverlay = AcquirePanel(panelId, entry);
            activeOverlay.OnPanelOpened(context ?? MenuContext.Empty);
        }

        public void CloseCurrentPanel()
        {
            if (activeOverlay == null)
                return;

            MenuPanelId panelId = activeOverlay.PanelId;
            activeOverlay.OnPanelClosed();

            if (activeOverlay.DestroyOnClose && !IsCached(panelId))
                Destroy(activeOverlay.gameObject);
            else
                activeOverlay.gameObject.SetActive(false);

            activeOverlay = null;
            mainMenuPanel?.Show();
        }

        public void ExecuteAction(IMenuAction action)
        {
            action?.Execute(this);
        }

        public void OpenMoreGames() => ExecuteAction(new MenuUrlAction(moreGamesUrl));

        public void OpenPrivacyPolicy() => ExecuteAction(new MenuUrlAction(privacyPolicyUrl));

        public void OpenRateUs() => ExecuteAction(new MenuUrlAction(rateUsUrl));

        public void OpenDiscord() => ExecuteAction(new MenuUrlAction(discordUrl));

        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError("MenuManager gameplay scene name is not set.", this);
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
            SceneManager.LoadScene("GamePlayEnv", LoadSceneMode.Additive);
        }

        private UIPanelBase AcquirePanel(MenuPanelId panelId, MenuPanelRegistry.PanelEntry entry)
        {
            if (entry.cacheInstance && cachedPanels.TryGetValue(panelId, out UIPanelBase cached) && cached != null)
            {
                cached.gameObject.SetActive(true);
                return cached;
            }

            UIPanelBase instance = Instantiate(entry.prefab, panelRoot);
            instance.BindMenuManager(this);

            if (entry.cacheInstance)
                cachedPanels[panelId] = instance;

            return instance;
        }

        private bool IsCached(MenuPanelId panelId)
        {
            return cachedPanels.ContainsKey(panelId);
        }
    }
}

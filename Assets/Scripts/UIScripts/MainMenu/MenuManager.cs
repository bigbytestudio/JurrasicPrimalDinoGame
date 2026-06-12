using System.Collections;
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
        [SerializeField] private CurrencyDisplayUI currencyDisplay;

        [Header("External Links")]
        [SerializeField] private string moreGamesUrl;
        [SerializeField] private string termsUrl;
        [SerializeField] private string supportUrl;
        [SerializeField] private string privacyPolicyUrl;
        [SerializeField] private string rateUsUrl;
        [SerializeField] private string discordUrl;

        [Header("Rate Us Prompt")]
        [SerializeField] private bool enableRandomRateUsPrompt = true;
        [SerializeField, Range(0f, 1f)] private float rateUsPromptChance = 0.35f;
        [SerializeField] private float rateUsPromptDelay = 0.65f;

        private readonly Dictionary<MenuPanelId, UIPanelBase> cachedPanels = new();
        private UIPanelBase activeOverlay;
        private Coroutine rateUsPromptRoutine;

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

            if (currencyDisplay == null)
                currencyDisplay = FindObjectOfType<CurrencyDisplayUI>();
        }

        private void Start()
        {
            ShowMainMenu();
            TryScheduleRateUsPrompt();
        }

        private void OnDestroy()
        {
            if (rateUsPromptRoutine != null)
                StopCoroutine(rateUsPromptRoutine);

            if (Instance == this)
                Instance = null;
        }

        public void ShowMainMenu()
        {
            CloseCurrentPanel();
            mainMenuPanel?.Show();
            UpdateCurrencyDisplayVisibility();
        }

        public void OpenPanel(MenuPanelId panelId, MenuContext context = null)
        {
            if (panelId == MenuPanelId.None || panelId == MenuPanelId.MainMenu)
            {
                ShowMainMenu();
                return;
            }

            if (activeOverlay != null && activeOverlay.PanelId == panelId)
            {
                if (TryToggleOrRefreshActivePanel(panelId, context))
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

            CloseCurrentPanel(showMainMenu: false);
            PresentOverlayPanel(panelId, context ?? MenuContext.Empty, entry);
        }

        public void CloseCurrentPanel(bool showMainMenu = true)
        {
            if (activeOverlay == null)
                return;

            MenuPanelId panelId = activeOverlay.PanelId;
            activeOverlay.OnPanelClosed();

            if (IsCached(panelId))
                activeOverlay.gameObject.SetActive(false);
            else
                Destroy(activeOverlay.gameObject);

            activeOverlay = null;

            if (showMainMenu)
                mainMenuPanel?.Show();

            UpdateCurrencyDisplayVisibility();
        }

        private void PresentOverlayPanel(MenuPanelId panelId, MenuContext context, MenuPanelRegistry.PanelEntry entry)
        {
            if (mainMenuPanel != null && mainMenuPanel.gameObject.activeInHierarchy)
            {
                mainMenuPanel.Hide(() => OpenOverlayAfterMainMenuHidden(panelId, context, entry));
                return;
            }

            OpenOverlayAfterMainMenuHidden(panelId, context, entry);
        }

        private void OpenOverlayAfterMainMenuHidden(
            MenuPanelId panelId,
            MenuContext context,
            MenuPanelRegistry.PanelEntry entry)
        {
            activeOverlay = AcquirePanel(panelId, entry);
            activeOverlay.OnPanelOpened(context);
            UpdateCurrencyDisplayVisibility();
        }

        public void ExecuteAction(IMenuAction action)
        {
            action?.Execute(this);
        }

        public void OpenMoreGames() => ExecuteAction(new MenuUrlAction(moreGamesUrl));

        public void OpenTerms() => ExecuteAction(new MenuUrlAction(termsUrl));

        public void OpenSupport() => ExecuteAction(new MenuUrlAction(supportUrl));

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

            MenuSessionContext.MarkLeftForGameplay();
            SceneManager.LoadScene(gameplaySceneName);
            SceneManager.LoadScene("GamePlayEnv", LoadSceneMode.Additive);
        }

        private void TryScheduleRateUsPrompt()
        {
            if (!enableRandomRateUsPrompt)
                return;

            if (!MenuSessionContext.TryConsumeReturnFromGameplay())
                return;

            if (Random.value > rateUsPromptChance)
                return;

            if (rateUsPromptRoutine != null)
                StopCoroutine(rateUsPromptRoutine);

            rateUsPromptRoutine = StartCoroutine(ShowRateUsPromptRoutine());
        }

        private IEnumerator ShowRateUsPromptRoutine()
        {
            if (rateUsPromptDelay > 0f)
                yield return new WaitForSeconds(rateUsPromptDelay);

            RateUsPopupController.Show();
            rateUsPromptRoutine = null;
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

        private bool TryToggleOrRefreshActivePanel(MenuPanelId panelId, MenuContext context)
        {
            if (panelId == MenuPanelId.Store && activeOverlay is StorePanel storePanel)
            {
                StoreTab requestedTab = context?.StoreTab ?? StoreTab.Bones;
                if (storePanel.ActiveTab == requestedTab)
                {
                    if (context?.ToggleCloseWhenSameTab ?? true)
                        CloseCurrentPanel();

                    return true;
                }

                storePanel.OnPanelOpened(context ?? MenuContext.Empty);
                UpdateCurrencyDisplayVisibility();
                return true;
            }

            CloseCurrentPanel();
            return true;
        }

        private void UpdateCurrencyDisplayVisibility()
        {
            if (currencyDisplay == null)
                return;

            bool hideCurrency = activeOverlay != null
                && (activeOverlay.PanelId == MenuPanelId.Settings
                    || activeOverlay.PanelId == MenuPanelId.Profile);

            currencyDisplay.gameObject.SetActive(!hideCurrency);
        }
    }
}

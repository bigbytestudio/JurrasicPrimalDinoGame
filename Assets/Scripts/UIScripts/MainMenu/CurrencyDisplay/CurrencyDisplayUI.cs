using SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    /// <summary>
    /// Single shared currency bar shown across all menu panels.
    /// Reads DNA and bones from <see cref="GameDataSave"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrencyDisplayUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text dnaAmountText;
        [SerializeField] private TMP_Text bonesAmountText;
        [SerializeField] private Button dnaButton;
        [SerializeField] private Button bonesButton;

        private void OnEnable()
        {
            EnsureGameDataLoaded();
            GameDataSave.CurrencyChanged += Refresh;
            BindButtons();
            Refresh();
        }

        private static void EnsureGameDataLoaded()
        {
            if (GameDataSave.Instance != null)
                return;

            GameDataSave gameData = GameDataSave.Load();
            GameDataSave.Bind(gameData);
            SaveDataService.Instance.Register(gameData);
        }

        private void OnDisable()
        {
            GameDataSave.CurrencyChanged -= Refresh;
        }

        private void BindButtons()
        {
            if (dnaButton != null)
            {
                dnaButton.onClick.RemoveAllListeners();
                dnaButton.onClick.AddListener(OpenDnaStore);
            }

            if (bonesButton != null)
            {
                bonesButton.onClick.RemoveAllListeners();
                bonesButton.onClick.AddListener(OpenBonesStore);
            }
        }

        public void Refresh()
        {
            GameDataSave data = GameDataSave.Instance;
            if (data == null)
                return;

            if (dnaAmountText != null)
                dnaAmountText.text = data.dnaCurrency.ToString();

            if (bonesAmountText != null)
                bonesAmountText.text = data.bonesCurrency.ToString();
        }

        private static void OpenDnaStore()
        {
            MenuManager.Instance?.OpenPanel(
                MenuPanelId.Store,
                MenuContext.ForStore(StoreTab.Dna, toggleCloseWhenSameTab: false));
        }

        private static void OpenBonesStore()
        {
            MenuManager.Instance?.OpenPanel(
                MenuPanelId.Store,
                MenuContext.ForStore(StoreTab.Bones, toggleCloseWhenSameTab: false));
        }
    }
}

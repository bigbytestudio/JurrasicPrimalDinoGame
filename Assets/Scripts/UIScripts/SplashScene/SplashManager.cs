using UnityEngine;
using SaveSystem;
using DinoGame.Data;
using DinoGame.UI.Menu;

public class SplashManager : MonoBehaviour
{
    public GameObject loadingScreen;

    public GameObject privacyPolicyPanel;

    [SerializeField] private CreatureRegistry creatureRegistryPrefab;

    void Start()
    {
        EnsureCreatureRegistry();
        EnsurePlayerProfileStatsTracker();
        ShowLoadingScreen(true);
        GameDataSave gameData = GameDataSave.Load();
        GameDataSave.Bind(gameData);
        SaveDataService.Instance.Register(gameData);

        SettingsSave settings = SettingsSave.Load();
        SettingsSave.Bind(settings);
        SaveDataService.Instance.Register(settings);
    }
    public void ShowLoadingScreen(bool show)
    {
        loadingScreen.SetActive(show);

    }


    public void ShowPrivacyPolicyPanel(bool show)
    {
        privacyPolicyPanel.SetActive(show);
    }

    private void EnsureCreatureRegistry()
    {
        if (CreatureRegistry.Instance != null)
            return;

        if (creatureRegistryPrefab != null)
            Instantiate(creatureRegistryPrefab);
        else
            CreatureRegistry.EnsureExists();
    }

    private void EnsurePlayerProfileStatsTracker()
    {
        PlayerProfileStatsTracker.EnsureExists();
    }
}

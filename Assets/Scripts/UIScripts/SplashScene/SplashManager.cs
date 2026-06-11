using UnityEngine;
using SaveSystem;

public class SplashManager : MonoBehaviour
{
   
    public GameObject loadingScreen;

    public GameObject privacyPolicyPanel;

    void Start()
    {
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


    public void ShowPrivacyPolicyPanel(bool show){
        privacyPolicyPanel.SetActive(show);
    }

   

}

using UnityEngine;

public class SplashManager : MonoBehaviour
{
   
    public GameObject loadingScreen;

    public GameObject privacyPolicyPanel;

    void Start()
    {
        ShowLoadingScreen(true);
    }
    public void ShowLoadingScreen(bool show)
    {
        loadingScreen.SetActive(show);

    }


    public void ShowPrivacyPolicyPanel(bool show){
        privacyPolicyPanel.SetActive(show);
    }

   

}

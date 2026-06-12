using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class LoadingScreen : MonoBehaviour
{
    public Image fillImage;

    private float activeTime;

    [SerializeField] private float maxTime;


    private void Update()
    {
        activeTime += Time.deltaTime;
        SetFillAmount(activeTime / maxTime);
        if (activeTime >= maxTime)
        {
            activeTime = 0;
            SceneManager.LoadScene("MainMenu");
            SceneManager.LoadScene("MainMenuEnv", LoadSceneMode.Additive);
        }
    }

    public void SetFillAmount(float amount)
    {
        fillImage.fillAmount = amount;
    }


}

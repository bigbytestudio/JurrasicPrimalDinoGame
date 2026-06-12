using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoGame.UI.Menu
{
    public static class GameplayMenuReturn
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string MainMenuEnvSceneName = "MainMenuEnv";

        public static void ReturnToMainMenu()
        {
            SceneManager.LoadScene(MainMenuSceneName);
            SceneManager.LoadScene(MainMenuEnvSceneName, LoadSceneMode.Additive);
        }
    }
}

using UnityEngine;

namespace DinoGame.Systems
{
    public sealed class SaveSystem : MonoBehaviour
    {
        private const string SoftCurrencyKey = "DinoGame.SoftCurrency";
        public int SoftCurrency
        {
            get => PlayerPrefs.GetInt(SoftCurrencyKey, 0);
            set
            {
                PlayerPrefs.SetInt(SoftCurrencyKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }
        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SoftCurrencyKey);
            PlayerPrefs.Save();
        }
    }
}

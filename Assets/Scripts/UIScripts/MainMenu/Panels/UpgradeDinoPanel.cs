using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class UpgradeDinoPanel : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text creatureNameText;

        private System.Action onClosed;

        public static UpgradeDinoPanel Open(Transform parent, GameObject prefab, CreatureProfile profile, System.Action onClosed)
        {
            if (parent == null || prefab == null)
                return null;

            GameObject instance = Instantiate(prefab, parent);
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            UpgradeDinoPanel panel = instance.GetComponent<UpgradeDinoPanel>();
            if (panel == null)
                panel = instance.AddComponent<UpgradeDinoPanel>();

            panel.Initialize(profile, onClosed);
            return panel;
        }

        private void Initialize(CreatureProfile profile, System.Action closedCallback)
        {
            onClosed = closedCallback;
            TryAutoBind();

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (creatureNameText != null && profile != null)
                creatureNameText.text = profile.displayName;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            onClosed?.Invoke();
            Destroy(gameObject);
        }

        private void TryAutoBind()
        {
            if (closeButton == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "closeBtn" || child.name == "closeButton")
                    {
                        closeButton = child.GetComponent<Button>();
                        if (closeButton != null)
                            break;
                    }
                }
            }
        }
    }
}

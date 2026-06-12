using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class DinoGrowthPanel : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button upgradeGrowthButton;
        [SerializeField] private TMP_Text creatureNameText;
        [SerializeField] private TMP_Text growthLevelText;

        private CreatureProfile profile;
        private System.Action onClosed;

        public static DinoGrowthPanel Open(Transform parent, GameObject prefab, CreatureProfile profile, System.Action onClosed)
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

            ProfilePanel profilePanel = instance.GetComponent<ProfilePanel>();
            if (profilePanel != null)
                profilePanel.enabled = false;

            DinoGrowthPanel panel = instance.GetComponent<DinoGrowthPanel>();
            if (panel == null)
                panel = instance.AddComponent<DinoGrowthPanel>();

            panel.Initialize(profile, onClosed);
            return panel;
        }

        private void Initialize(CreatureProfile creatureProfile, System.Action closedCallback)
        {
            profile = creatureProfile;
            onClosed = closedCallback;
            TryAutoBind();
            RefreshGrowthDisplay();

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (upgradeGrowthButton != null)
                upgradeGrowthButton.onClick.AddListener(HandleUpgradeGrowth);

            if (creatureNameText != null && profile != null)
                creatureNameText.text = profile.displayName;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (upgradeGrowthButton != null)
                upgradeGrowthButton.onClick.RemoveListener(HandleUpgradeGrowth);
        }

        private void HandleUpgradeGrowth()
        {
            if (profile == null || GrowthUpgradeUtility.IsMaxGrowth(profile))
                return;

            GrowthUpgradePopupController.ShowConfirmation(profile, RefreshGrowthDisplay);
        }

        private void RefreshGrowthDisplay()
        {
            if (profile == null)
                return;

            if (growthLevelText != null)
            {
                string stageLabel = GrowthUpgradeUtility.GetStageLabel(profile);
                int growthLevel = GrowthUpgradeUtility.GetGrowthLevel(profile);
                growthLevelText.text = $"● {stageLabel} {growthLevel}";
            }
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
                    if (child.name != "closeBtn")
                        continue;

                    closeButton = child.GetComponent<Button>();
                    break;
                }
            }

            if (creatureNameText == null)
            {
                TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].text == "DINO PROFILE")
                    {
                        creatureNameText = texts[i];
                        break;
                    }
                }
            }

            if (upgradeGrowthButton == null)
            {
                Transform upgradeTransform = transform.Find("Popup/upgradeGrowthBtn");
                if (upgradeTransform != null)
                    upgradeGrowthButton = upgradeTransform.GetComponent<Button>();
            }

            if (growthLevelText == null)
            {
                Transform growthStage = transform.Find("Popup/GROWTH STAGE");
                if (growthStage != null)
                    growthLevelText = growthStage.GetComponent<TMP_Text>();
            }
        }
    }
}

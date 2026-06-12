using UnityEngine;
using UnityEngine.UI;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class DinoGrowthPanel : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button upgradeGrowthButton;
        [SerializeField] private DinoGrowthPanelView panelView;

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
            RefreshDisplay();

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (upgradeGrowthButton != null)
                upgradeGrowthButton.onClick.AddListener(HandleUpgradeGrowth);
        }

        private void OnEnable()
        {
            GrowthUpgradeUtility.GrowthLevelChanged += HandleGrowthLevelChanged;
        }

        private void OnDisable()
        {
            GrowthUpgradeUtility.GrowthLevelChanged -= HandleGrowthLevelChanged;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (upgradeGrowthButton != null)
                upgradeGrowthButton.onClick.RemoveListener(HandleUpgradeGrowth);
        }

        private void HandleGrowthLevelChanged()
        {
            RefreshDisplay();
        }

        private void HandleUpgradeGrowth()
        {
            if (profile == null || GrowthUpgradeUtility.IsMaxGrowth(profile))
                return;

            GrowthUpgradePopupController.ShowConfirmation(profile, RefreshDisplay);
        }

        private void RefreshDisplay()
        {
            panelView?.Bind(profile);
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

            if (panelView == null)
                panelView = GetComponent<DinoGrowthPanelView>();

            if (panelView == null)
                panelView = gameObject.AddComponent<DinoGrowthPanelView>();

            if (upgradeGrowthButton == null)
            {
                Transform upgradeTransform = transform.Find("Popup/upgradeGrowthBtn");
                if (upgradeTransform != null)
                    upgradeGrowthButton = upgradeTransform.GetComponent<Button>();
            }
        }
    }
}

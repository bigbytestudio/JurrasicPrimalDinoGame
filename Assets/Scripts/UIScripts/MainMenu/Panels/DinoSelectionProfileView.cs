using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class DinoSelectionProfileView : MonoBehaviour
    {
        [SerializeField] private TMP_Text creatureCodeText;
        [SerializeField] private TMP_Text creatureNameText;
        [SerializeField] private TMP_Text infamyLevelText;
        [SerializeField] private TMP_Text infamyTierText;
        [SerializeField] private TMP_Text attackPercentText;
        [SerializeField] private TMP_Text defensePercentText;
        [SerializeField] private TMP_Text staminaPercentText;
        [SerializeField] private TMP_Text growthLevelText;
        [SerializeField] private Image previewIconImage;

        private void Awake()
        {
            TryAutoBind();
        }

        public void Bind(CreatureProfile profile)
        {
            if (profile == null)
                return;

            if (creatureCodeText != null)
                creatureCodeText.text = profile.GetCreatureCode();

            if (creatureNameText != null)
                creatureNameText.text = profile.displayName;

            if (infamyLevelText != null)
                infamyLevelText.text = $"INFAMY LEVEL {profile.infamyLevel}";

            if (infamyTierText != null)
                infamyTierText.text = profile.infamyTierLabel;

            if (attackPercentText != null)
                attackPercentText.text = FormatPercent(profile.attackPercent);

            if (defensePercentText != null)
                defensePercentText.text = FormatPercent(profile.defensePercent);

            if (staminaPercentText != null)
                staminaPercentText.text = FormatPercent(profile.staminaPercent);

            if (growthLevelText != null)
            {
                string stageLabel = GrowthUpgradeUtility.GetStageLabel(profile);
                int growthLevel = GrowthUpgradeUtility.GetGrowthLevel(profile);
                growthLevelText.text = $"● {stageLabel} {growthLevel}";
            }

            if (previewIconImage != null && profile.previewIcon != null)
            {
                previewIconImage.sprite = profile.previewIcon;
                previewIconImage.enabled = true;
            }
        }

        private static string FormatPercent(int value) => $"{value} %";

        private void TryAutoBind()
        {
            Transform statsPanel = transform.Find("StatsPanel");
            if (statsPanel != null)
            {
                Transform profileData = statsPanel.Find("ProfileData");
                if (profileData != null)
                {
                    Transform profileName = profileData.Find("profileName");
                    if (profileName != null)
                    {
                        TMP_Text[] nameTexts = profileName.GetComponentsInChildren<TMP_Text>(true);
                        if (creatureCodeText == null && nameTexts.Length > 0)
                            creatureCodeText = nameTexts[0];
                        if (creatureNameText == null && nameTexts.Length > 1)
                            creatureNameText = nameTexts[1];
                    }

                    Transform profilePictureSetup = profileData.Find("profilePictureSetup");
                    if (profilePictureSetup != null && previewIconImage == null)
                    {
                        Transform profilePic = profilePictureSetup.Find("profilePic");
                        if (profilePic != null)
                            previewIconImage = profilePic.GetComponent<Image>();
                    }
                }

                Transform stats = statsPanel.Find("Stats");
                if (stats != null)
                {
                    if (attackPercentText == null)
                        attackPercentText = FindPercentText(stats, "attackRow");
                    if (defensePercentText == null)
                        defensePercentText = FindPercentText(stats, "defenseRow");
                    if (staminaPercentText == null)
                        staminaPercentText = FindPercentText(stats, "StaminaRow");
                }

                Transform infamyLevels = statsPanel.Find("infamyLevels");
                if (infamyLevels != null)
                {
                    if (infamyLevelText == null)
                    {
                        TMP_Text[] infamyTexts = infamyLevels.GetComponentsInChildren<TMP_Text>(true);
                        for (int i = 0; i < infamyTexts.Length; i++)
                        {
                            TMP_Text candidate = infamyTexts[i];
                            if (candidate.name != "infamyLevelTxt")
                                continue;

                            if (candidate.text != "INFAMY LEVEL")
                            {
                                infamyLevelText = candidate;
                                break;
                            }
                        }
                    }

                    if (infamyTierText == null)
                    {
                        Transform tierTransform = infamyLevels.Find("harmLessTxt");
                        if (tierTransform != null)
                            infamyTierText = tierTransform.GetComponent<TMP_Text>();
                    }
                }
            }

            if (growthLevelText == null)
            {
                Transform growthStage = transform.Find("RightSide/dinoGrowthTxt/developmentStage");
                if (growthStage != null)
                    growthLevelText = growthStage.GetComponent<TMP_Text>();
            }
        }

        private static TMP_Text FindPercentText(Transform statsRoot, string rowName)
        {
            Transform row = statsRoot.Find(rowName);
            if (row == null)
                return null;

            Transform percentTransform = row.Find("percentTxt");
            return percentTransform != null ? percentTransform.GetComponent<TMP_Text>() : null;
        }
    }
}

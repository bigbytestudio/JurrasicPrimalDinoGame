using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class DinoSelectionProfileView : MonoBehaviour
    {
        [Header("Fill Bar Animation")]
        [SerializeField] private float fillAnimDuration = 0.65f;
        [SerializeField] private float fillStagger = 0.1f;

        [SerializeField] private TMP_Text creatureCodeText;
        [SerializeField] private TMP_Text creatureNameText;
        [SerializeField] private TMP_Text infamyLevelText;
        [SerializeField] private TMP_Text infamyTierText;
        [SerializeField] private TMP_Text attackPercentText;
        [SerializeField] private TMP_Text defensePercentText;
        [SerializeField] private TMP_Text staminaPercentText;
        [SerializeField] private TMP_Text growthLevelText;
        [SerializeField] private Image previewIconImage;
        [SerializeField] private Image attackFillBar;
        [SerializeField] private Image defenseFillBar;
        [SerializeField] private Image staminaFillBar;

        private readonly List<Coroutine> fillAnimations = new();

        private void Awake()
        {
            TryAutoBind();
        }

        private void OnDisable()
        {
            UIFillBarAnimator.StopAll(fillAnimations, this);
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

            if (growthLevelText != null)
            {
                string stageLabel = GrowthUpgradeUtility.GetStageLabel(profile);
                int growthLevel = GrowthUpgradeUtility.GetGrowthLevel(profile);
                growthLevelText.text = $"● {stageLabel} {growthLevel}";
            }

            CreaturePortraitUtility.ApplyProfilePortrait(previewIconImage, profile);

            UIFillBarAnimator.StopAll(fillAnimations, this);

            float delay = 0f;
            AnimateStatRow(attackFillBar, attackPercentText, profile.attackPercent, delay);
            delay += fillStagger;
            AnimateStatRow(defenseFillBar, defensePercentText, profile.defensePercent, delay);
            delay += fillStagger;
            AnimateStatRow(staminaFillBar, staminaPercentText, profile.staminaPercent, delay);
        }

        private void AnimateStatRow(Image fillBar, TMP_Text percentText, int targetPercent, float delay)
        {
            UIFillBarAnimator.Animate(
                this,
                fillAnimations,
                fillBar,
                percentText,
                targetPercent / 100f,
                targetPercent,
                delay,
                fillAnimDuration);
        }

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
                    if (attackPercentText == null || attackFillBar == null)
                        BindStatRow(stats, "attackRow", ref attackPercentText, ref attackFillBar);
                    if (defensePercentText == null || defenseFillBar == null)
                        BindStatRow(stats, "defenseRow", ref defensePercentText, ref defenseFillBar);
                    if (staminaPercentText == null || staminaFillBar == null)
                        BindStatRow(stats, "StaminaRow", ref staminaPercentText, ref staminaFillBar);
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

        private static void BindStatRow(
            Transform statsRoot,
            string rowName,
            ref TMP_Text percentText,
            ref Image fillBar)
        {
            Transform row = statsRoot.Find(rowName);
            if (row == null)
                return;

            if (percentText == null)
                percentText = FindPercentText(row);

            if (fillBar == null)
                fillBar = FindFillBar(row);
        }

        private static TMP_Text FindPercentText(Transform row)
        {
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name.ToLowerInvariant().Contains("percent"))
                    return texts[i];
            }

            Transform percentTransform = row.Find("percentTxt");
            return percentTransform != null ? percentTransform.GetComponent<TMP_Text>() : null;
        }

        private static Image FindFillBar(Transform row)
        {
            Image[] images = row.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "FillBar")
                    return images[i];
            }

            return null;
        }
    }
}

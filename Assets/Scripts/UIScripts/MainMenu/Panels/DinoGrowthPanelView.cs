using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class DinoGrowthPanelView : MonoBehaviour
    {
        [Header("Fill Bar Animation")]
        [SerializeField] private float fillAnimDuration = 0.65f;
        [SerializeField] private float fillStagger = 0.07f;

        [Header("Stage Glow Pulse")]
        [SerializeField] private float glowPulseScale = 0.06f;
        [SerializeField] private float glowPulseSpeed = 2.4f;

        private TMP_Text nameText;
        private TMP_Text typeText;
        private TMP_Text ageStageText;
        private TMP_Text sizeText;
        private Image stageProgressFill;

        private readonly List<StageVisualBinding> stageVisuals = new();
        private readonly Dictionary<string, Image> statFillBars = new();
        private readonly Dictionary<string, Image> lifeFillBars = new();
        private readonly List<Coroutine> fillAnimations = new();

        private Coroutine glowPulseRoutine;
        private Transform activeGlowTransform;
        private Vector3 activeGlowBaseScale = Vector3.one;

        private void Awake()
        {
            TryAutoBind();
        }

        private void OnDisable()
        {
            StopFillAnimations();
            StopGlowPulse();
        }

        public void Bind(CreatureProfile profile)
        {
            if (profile == null)
                return;

            int growthLevel = GrowthUpgradeUtility.GetGrowthLevel(profile);
            string stageLabel = GrowthUpgradeUtility.GetStageLabel(profile);

            if (nameText != null)
                nameText.text = profile.displayName;

            if (typeText != null)
                typeText.text = profile.dietTypeLabel;

            if (ageStageText != null)
                ageStageText.text = stageLabel;

            if (sizeText != null)
                sizeText.text = $"{profile.GetSizeMetersForGrowthLevel(growthLevel):0.#}m";

            StopFillAnimations();
            AnimateFill(stageProgressFill, GetStageProgressFill(profile, growthLevel), 0f);

            float delay = fillStagger;
            AnimateFill(statFillBars, "health", profile.growthHealthFill, delay);
            delay += fillStagger;
            AnimateFill(statFillBars, "damage", profile.growthDamageFill, delay);
            delay += fillStagger;
            AnimateFill(statFillBars, "speed", profile.growthSpeedFill, delay);
            delay += fillStagger;
            AnimateFill(statFillBars, "swim", profile.growthSwimFill, delay);
            delay += fillStagger;
            AnimateFill(lifeFillBars, "sleep", profile.lifeSleepFill, delay);
            delay += fillStagger;
            AnimateFill(lifeFillBars, "water", profile.lifeWaterFill, delay);
            delay += fillStagger;
            AnimateFill(lifeFillBars, "hunger", profile.lifeHungerFill, delay);

            ApplyStageVisuals(growthLevel);
        }

        private void TryAutoBind()
        {
            Transform popup = transform.Find("Popup");
            if (popup == null)
                popup = transform;

            BindProfileTexts(popup);
            BindStageSystem(popup.Find("StageSystem"));
            BindMetricRows(popup.Find("statsOverview"), statFillBars);
            BindMetricRows(popup.Find("LifeStatus"), lifeFillBars);
        }

        private void BindProfileTexts(Transform popup)
        {
            Transform profileData = popup.Find("profileDataBox");
            if (profileData == null)
                return;

            nameText ??= FindText(profileData, "Row_1/nameTxt");
            typeText ??= FindText(profileData, "Row_2/typeTxt");
            ageStageText ??= FindText(profileData, "Row_3/ageStageTxt");
            sizeText ??= FindText(profileData, "Row_4/sizeTxt");
        }

        private void BindStageSystem(Transform stageSystem)
        {
            if (stageSystem == null)
                return;

            stageProgressFill ??= FindFillBar(stageSystem);

            if (stageVisuals.Count > 0)
                return;

            for (int i = 1; i <= 4; i++)
            {
                Transform stage = stageSystem.Find($"Stage_{i}");
                if (stage == null)
                    continue;

                Transform withGlow = stage.Find("withGlow");
                Transform withoutGlow = stage.Find("withoutGlow");
                if (withGlow == null || withoutGlow == null)
                    continue;

                stageVisuals.Add(new StageVisualBinding
                {
                    withGlow = withGlow.gameObject,
                    withoutGlow = withoutGlow.gameObject,
                    glowTransform = withGlow,
                    glowBaseScale = withGlow.localScale
                });
            }
        }

        private static void BindMetricRows(Transform sectionRoot, Dictionary<string, Image> target)
        {
            if (sectionRoot == null || target.Count > 0)
                return;

            foreach (Transform row in sectionRoot)
            {
                if (!row.name.StartsWith("row", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                TMP_Text heading = row.GetComponentInChildren<TMP_Text>(true);
                if (heading == null)
                    continue;

                string key = heading.text.Trim().ToLowerInvariant();
                Image fillBar = FindFillBar(row);
                if (fillBar == null)
                    continue;

                target[key] = fillBar;
            }
        }

        private void ApplyStageVisuals(int activeGrowthLevel)
        {
            StopGlowPulse();

            int activeIndex = Mathf.Clamp(activeGrowthLevel, 1, stageVisuals.Count) - 1;

            for (int i = 0; i < stageVisuals.Count; i++)
            {
                bool isActiveStage = i == activeIndex;
                StageVisualBinding stage = stageVisuals[i];

                if (stage.withGlow != null)
                {
                    stage.withGlow.SetActive(isActiveStage);
                    if (isActiveStage)
                        stage.glowTransform.localScale = stage.glowBaseScale;
                }

                if (stage.withoutGlow != null)
                    stage.withoutGlow.SetActive(!isActiveStage);
            }

            if (activeIndex >= 0 && activeIndex < stageVisuals.Count)
            {
                StageVisualBinding activeStage = stageVisuals[activeIndex];
                StartGlowPulse(activeStage.glowTransform, activeStage.glowBaseScale);
            }
        }

        private void AnimateFill(Dictionary<string, Image> fills, string key, float amount, float delay)
        {
            if (!fills.TryGetValue(key, out Image fillBar))
                return;

            AnimateFill(fillBar, amount, delay);
        }

        private void AnimateFill(Image image, float target, float delay)
        {
            if (image == null)
                return;

            PrepareFillImage(image);
            image.fillAmount = 0f;
            fillAnimations.Add(StartCoroutine(AnimateFillRoutine(image, target, delay)));
        }

        private IEnumerator AnimateFillRoutine(Image image, float target, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            const float start = 0f;
            float elapsed = 0f;

            while (elapsed < fillAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fillAnimDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                image.fillAmount = Mathf.Lerp(start, target, t);
                yield return null;
            }

            image.fillAmount = Mathf.Clamp01(target);
        }

        private void StartGlowPulse(Transform glowTransform, Vector3 baseScale)
        {
            if (glowTransform == null)
                return;

            activeGlowTransform = glowTransform;
            activeGlowBaseScale = baseScale;
            glowPulseRoutine = StartCoroutine(GlowPulseRoutine());
        }

        private IEnumerator GlowPulseRoutine()
        {
            while (activeGlowTransform != null)
            {
                float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * glowPulseScale;
                activeGlowTransform.localScale = activeGlowBaseScale * (1f + pulse);
                yield return null;
            }
        }

        private void StopFillAnimations()
        {
            for (int i = 0; i < fillAnimations.Count; i++)
            {
                if (fillAnimations[i] != null)
                    StopCoroutine(fillAnimations[i]);
            }

            fillAnimations.Clear();
        }

        private void StopGlowPulse()
        {
            if (glowPulseRoutine != null)
            {
                StopCoroutine(glowPulseRoutine);
                glowPulseRoutine = null;
            }

            if (activeGlowTransform != null)
            {
                activeGlowTransform.localScale = activeGlowBaseScale;
                activeGlowTransform = null;
            }
        }

        private static float GetStageProgressFill(CreatureProfile profile, int growthLevel)
        {
            int maxLevel = Mathf.Max(1, profile.maxGrowthLevel);
            if (maxLevel <= 1)
                return 1f;

            return Mathf.Clamp01((growthLevel - 1f) / (maxLevel - 1f));
        }

        private static void PrepareFillImage(Image image)
        {
            image.type = Image.Type.Filled;
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private static Image FindFillBar(Transform root)
        {
            Transform fillTransform = root.Find("FillBar");
            if (fillTransform != null)
                return fillTransform.GetComponent<Image>();

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "FillBar")
                    return images[i];
            }

            return null;
        }

        private sealed class StageVisualBinding
        {
            public GameObject withGlow;
            public GameObject withoutGlow;
            public Transform glowTransform;
            public Vector3 glowBaseScale = Vector3.one;
        }
    }
}

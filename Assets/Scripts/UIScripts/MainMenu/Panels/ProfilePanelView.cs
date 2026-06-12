using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class ProfilePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text dnaAmountText;
        [SerializeField] private TMP_Text bonesAmountText;
        [SerializeField] private TMP_Text timeSpentText;
        [SerializeField] private TMP_Text dinoKillsText;
        [SerializeField] private TMP_Text unlockedDinosText;
        [SerializeField] private TMP_Text rankNumberText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text dnaBonusPercentText;
        [SerializeField] private TMP_Text bonesBonusPercentText;
        [SerializeField] private Image rankFillBar;

        [Header("Fill Bar Animation")]
        [SerializeField] private float rankFillAnimDuration = 0.55f;

        private Coroutine rankFillRoutine;

        private void Awake()
        {
            TryAutoBind();
        }

        private void OnEnable()
        {
            GameDataSave.CurrencyChanged += Refresh;
            GameDataSave.ProfileStatsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameDataSave.CurrencyChanged -= Refresh;
            GameDataSave.ProfileStatsChanged -= Refresh;

            if (rankFillRoutine != null)
            {
                StopCoroutine(rankFillRoutine);
                rankFillRoutine = null;
            }
        }

        public void Refresh()
        {
            GameDataSave data = GameDataSave.Instance;
            if (data == null)
                return;

            int totalCreatures = ResolveTotalCreatureCount();

            if (playerNameText != null)
                playerNameText.text = data.playerName;

            if (dnaAmountText != null)
                dnaAmountText.text = data.dnaCurrency.ToString();

            if (bonesAmountText != null)
                bonesAmountText.text = data.bonesCurrency.ToString();

            if (timeSpentText != null)
                timeSpentText.text = FormatPlayTime(GetDisplayedPlayTimeSeconds(data));

            if (dinoKillsText != null)
                dinoKillsText.text = data.totalDinoKills.ToString();

            if (unlockedDinosText != null)
                unlockedDinosText.text = $"{data.GetUnlockedCreatureCount()} / {totalCreatures}";

            if (rankNumberText != null)
                rankNumberText.text = data.playerRank.ToString();

            if (xpText != null)
                xpText.text = $"{data.playerXp}/{data.xpPerRank}";

            if (dnaBonusPercentText != null)
                dnaBonusPercentText.text = $"+{data.GetDnaBonusPercent(totalCreatures)}%";

            if (bonesBonusPercentText != null)
                bonesBonusPercentText.text = $"+{data.GetBonesBonusPercent(totalCreatures)}%";

            AnimateRankFill(data.GetRankProgress01());
        }

        private void AnimateRankFill(float targetFill)
        {
            if (rankFillBar == null)
                return;

            if (rankFillRoutine != null)
                StopCoroutine(rankFillRoutine);

            rankFillBar.type = Image.Type.Filled;
            rankFillRoutine = StartCoroutine(AnimateRankFillRoutine(targetFill));
        }

        private System.Collections.IEnumerator AnimateRankFillRoutine(float targetFill)
        {
            float start = rankFillBar.fillAmount;
            float elapsed = 0f;

            while (elapsed < rankFillAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rankFillAnimDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                rankFillBar.fillAmount = Mathf.Lerp(start, targetFill, t);
                yield return null;
            }

            rankFillBar.fillAmount = targetFill;
            rankFillRoutine = null;
        }

        private static int ResolveTotalCreatureCount()
        {
            if (CreatureRegistry.Instance != null)
                return CreatureRegistry.Instance.Creatures.Length;

            return 0;
        }

        private static float GetDisplayedPlayTimeSeconds(GameDataSave data)
        {
            float seconds = data.totalPlayTimeSeconds;
            if (PlayerProfileStatsTracker.Instance != null)
                seconds += PlayerProfileStatsTracker.Instance.PendingPlayTimeSeconds;

            return seconds;
        }

        private static string FormatPlayTime(float totalSeconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int seconds = total % 60;

            if (hours > 0)
                return $"{hours}h {minutes}m {seconds}s";

            if (minutes > 0)
                return $"{minutes}m {seconds}s";

            return $"{seconds}s";
        }

        private void TryAutoBind()
        {
            Transform popup = transform.Find("Popup");
            if (popup == null)
                popup = transform;

            if (dnaAmountText == null)
                dnaAmountText = FindText(popup, "dnaBox/dnaTxt");

            if (bonesAmountText == null)
                bonesAmountText = FindText(popup, "bonesBox/boneTxt");

            Transform playerStats = popup.Find("PlayerStats/BG");
            if (playerStats != null)
            {
                if (timeSpentText == null)
                    timeSpentText = FindText(playerStats, "GameObject (1)/timeTxt");

                if (dinoKillsText == null)
                    dinoKillsText = FindText(playerStats, "killbox/killTxt");

                if (unlockedDinosText == null)
                    unlockedDinosText = FindText(playerStats, "GameObject (2)/unlockTxt");
            }

            Transform nameBox = popup.Find("nameBox");
            if (nameBox != null)
            {
                if (playerNameText == null)
                    playerNameText = FindText(nameBox, "playerNameTxt");

                if (rankNumberText == null)
                    rankNumberText = FindText(nameBox, "rankNumber");

                if (xpText == null)
                    xpText = FindText(nameBox, "xpTxt");

                if (rankFillBar == null)
                    rankFillBar = FindImage(nameBox, "fillbarLevel");
            }

            Transform bonusBox = popup.Find("BonusBox");
            if (bonusBox != null)
            {
                if (dnaBonusPercentText == null)
                    dnaBonusPercentText = FindText(bonusBox, "Row_1/%Txt");

                if (bonesBonusPercentText == null)
                    bonesBonusPercentText = FindText(bonusBox, "Row_3/%Txt");
            }
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<Image>() : null;
        }
    }
}

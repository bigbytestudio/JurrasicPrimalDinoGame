using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace DinoGame.UI.Menu
{
    public sealed class MainMenuProfileView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private Slider rankProgressSlider;
        [SerializeField] private float rankFillAnimDuration = 0.55f;

        private Tween rankFillTween;

        private void Awake()
        {
            TryAutoBind();
        }

        private void OnEnable()
        {
            GameDataSave.ProfileStatsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameDataSave.ProfileStatsChanged -= Refresh;
            rankFillTween?.Kill();
            rankFillTween = null;
        }

        public void Refresh()
        {
            GameDataSave data = GameDataSave.Instance;
            if (data == null)
                return;

            if (playerNameText != null)
                playerNameText.text = data.playerName;

            if (xpText != null)
                xpText.text = $"{data.playerXp}/{data.xpPerRank}";

            AnimateRankProgress(data.GetRankProgress01());
        }

        private void AnimateRankProgress(float targetFill)
        {
            if (rankProgressSlider == null)
                return;

            rankFillTween?.Kill();

            rankFillTween = DOTween
                .To(() => rankProgressSlider.value, value => rankProgressSlider.value = value, targetFill, rankFillAnimDuration)
                .SetEase(Ease.OutCubic)
                .SetTarget(rankProgressSlider);
        }

        private void TryAutoBind()
        {
            Transform profileBtn = transform.Find("MainPopup/ProfileBtn");
            if (profileBtn == null)
                profileBtn = transform.Find("ProfileBtn");

            if (profileBtn == null)
                return;

            if (playerNameText == null)
                playerNameText = FindText(profileBtn, "playerNameTxt");

            if (xpText == null)
                xpText = FindText(profileBtn, "scoreTxt");

            if (rankProgressSlider == null)
            {
                Transform sliderTransform = profileBtn.Find("ProfileFillBar/progressBar");
                if (sliderTransform != null)
                    rankProgressSlider = sliderTransform.GetComponent<Slider>();
            }
        }

        private static TMP_Text FindText(Transform root, string childName)
        {
            Transform target = root.Find(childName);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }
    }
}

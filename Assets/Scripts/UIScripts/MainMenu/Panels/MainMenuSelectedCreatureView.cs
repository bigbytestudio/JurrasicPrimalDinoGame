using UnityEngine;
using UnityEngine.UI;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class MainMenuSelectedCreatureView : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;

        private void Awake()
        {
            TryAutoBind();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            CreatureProfile profile = SelectedCreatureUtility.GetSelectedProfile();
            CreaturePortraitUtility.ApplyProfilePortrait(portraitImage, profile);
        }

        private void TryAutoBind()
        {
            if (portraitImage != null)
                return;

            Transform iconTransform = transform.Find("MainPopup/ProfileBtn/ProfileButton/Image");
            if (iconTransform == null)
                iconTransform = transform.Find("ProfileBtn/ProfileButton/Image");

            if (iconTransform != null)
                portraitImage = iconTransform.GetComponent<Image>();
        }
    }
}

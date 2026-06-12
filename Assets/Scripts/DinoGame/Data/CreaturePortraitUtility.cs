using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.Data
{
    public static class CreaturePortraitUtility
    {
        public static void ApplyProfilePortrait(Image image, CreatureProfile profile)
        {
            if (image == null)
                return;

            Sprite sprite = profile != null ? profile.GetProfilePortrait() : null;
            if (sprite == null)
            {
                image.enabled = false;
                return;
            }

            image.sprite = sprite;
            image.enabled = true;
        }

        public static void ApplyCardPortrait(Image image, CreatureProfile profile)
        {
            if (image == null)
                return;

            Sprite sprite = profile != null ? profile.GetCardPortrait() : null;
            if (sprite == null)
            {
                image.enabled = false;
                return;
            }

            image.sprite = sprite;
            image.enabled = true;

            if (profile.TryGetCardPortraitSize(out Vector2 size))
                image.rectTransform.sizeDelta = size;
        }
    }
}

using System;
using UnityEngine;

namespace DinoGame.Data
{
    [Serializable]
    public struct LocalizedText
    {
        public string key;
        [TextArea] public string value;
    }

    [CreateAssetMenu(menuName = "Dino Game/Data/Localization Table", fileName = "LocalizationTable")]
    public sealed class LocalizationTable : ScriptableObject
    {
        public string languageCode = "en";
        public LocalizedText[] texts;

        public string Get(string key)
        {
            if (texts == null) return key;
            for (int i = 0; i < texts.Length; i++)
                if (texts[i].key == key) return texts[i].value;
            return key;
        }
    }
}

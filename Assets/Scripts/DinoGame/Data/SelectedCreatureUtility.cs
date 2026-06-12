using UnityEngine;

namespace DinoGame.Data
{
    public static class SelectedCreatureUtility
    {
        public const string SelectedCreaturePrefsKey = "DinoGame.SelectedCreatureId";

        public static CreatureProfile GetSelectedProfile()
        {
            string creatureId = PlayerPrefs.GetString(SelectedCreaturePrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(creatureId))
            {
                CreatureProfile profile = ResolveProfile(creatureId);
                if (profile != null)
                    return profile;
            }

            return GetFirstAvailableProfile();
        }

        private static CreatureProfile ResolveProfile(string creatureId)
        {
            if (CreatureRegistry.Instance != null)
            {
                CreatureProfile profile = CreatureRegistry.Instance.FindById(creatureId);
                if (profile != null)
                    return profile;
            }

            return null;
        }

        private static CreatureProfile GetFirstAvailableProfile()
        {
            if (CreatureRegistry.Instance == null)
                return null;

            CreatureProfile[] creatures = CreatureRegistry.Instance.Creatures;
            return creatures.Length > 0 ? creatures[0] : null;
        }
    }
}

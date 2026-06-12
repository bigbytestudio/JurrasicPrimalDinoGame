using System;
using UnityEngine;

namespace DinoGame.Data
{
    /// <summary>
    /// Persistent catalog of all playable creatures. Shared by menu selection and gameplay spawning.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CreatureRegistry : MonoBehaviour
    {
        public static CreatureRegistry Instance { get; private set; }

        [SerializeField] private CreatureProfile[] creatures = Array.Empty<CreatureProfile>();

        public CreatureProfile[] Creatures => creatures ?? Array.Empty<CreatureProfile>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static CreatureRegistry EnsureExists(CreatureProfile[] fallbackCreatures = null)
        {
            if (Instance != null)
                return Instance;

            CreatureRegistry existing = FindObjectOfType<CreatureRegistry>();
            if (existing != null)
                return existing;

            GameObject host = new GameObject(nameof(CreatureRegistry));
            CreatureRegistry created = host.AddComponent<CreatureRegistry>();
            if (fallbackCreatures != null && fallbackCreatures.Length > 0)
                created.creatures = fallbackCreatures;

            return created;
        }

        public CreatureProfile FindById(string creatureId)
        {
            if (string.IsNullOrWhiteSpace(creatureId) || creatures == null)
                return null;

            for (int i = 0; i < creatures.Length; i++)
            {
                CreatureProfile profile = creatures[i];
                if (profile != null && profile.creatureId == creatureId)
                    return profile;
            }

            return null;
        }

        public bool TryGetByIndex(int index, out CreatureProfile profile)
        {
            profile = null;
            if (creatures == null || index < 0 || index >= creatures.Length)
                return false;

            profile = creatures[index];
            return profile != null;
        }
    }
}

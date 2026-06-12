
using DinoGame.Core;
using DinoGame.Systems;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    [DisallowMultipleComponent]
    public sealed class PlayerProfileStatsTracker : MonoBehaviour
    {
        public static PlayerProfileStatsTracker Instance { get; private set; }

        [SerializeField] private float playTimeSaveInterval = 5f;

        private float playTimeAccumulator;
        private float playTimeSaveTimer;

        public float PendingPlayTimeSeconds => playTimeAccumulator;

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

        private void OnEnable()
        {
            GameEventBus.CreatureDied += OnCreatureDied;
        }

        private void OnDisable()
        {
            GameEventBus.CreatureDied -= OnCreatureDied;
            FlushPlayTime();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            playTimeAccumulator += Time.unscaledDeltaTime;
            playTimeSaveTimer += Time.unscaledDeltaTime;

            if (playTimeSaveTimer < playTimeSaveInterval)
                return;

            FlushPlayTime();
        }

        public static void EnsureExists()
        {
            if (Instance != null)
                return;

            FindObjectOfType<PlayerProfileStatsTracker>();
            if (Instance != null)
                return;

            new GameObject(nameof(PlayerProfileStatsTracker)).AddComponent<PlayerProfileStatsTracker>();
        }

        private static void OnCreatureDied(DinoGame.Core.Creature creature, GameObject source)
        {
            if (creature == null || source == null)
                return;

            if (!source.TryGetComponent(out DinoGame.Core.Creature killer))
                return;

            if (killer.TeamId != (int)TeamType.Player)
                return;

            if (creature.TeamId == (int)TeamType.Player)
                return;

            GameDataSave.Instance?.RegisterPlayerKill();
        }

        private void FlushPlayTime()
        {
            if (playTimeAccumulator <= 0f)
                return;

            GameDataSave.Instance?.AddPlayTime(playTimeAccumulator);
            playTimeAccumulator = 0f;
            playTimeSaveTimer = 0f;
        }
    }
}

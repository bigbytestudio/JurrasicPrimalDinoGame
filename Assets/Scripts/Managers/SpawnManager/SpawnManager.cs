using System.Collections;
using UnityEngine;
using DinoGame.Core;
using DinoGame.Data;
using CoreCreature = DinoGame.Core.Creature;
using DinoGame.Input;
using DinoGame.Systems;

namespace DinoGame.Spawn
{
    /// <summary>
    /// Central spawn orchestrator for the player and zone-driven AI.
    /// Player prefabs come from selectable CoreCreature Profiles; AI is lazily activated per SpawnZone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnManager : MonoBehaviour
    {
        public const string SelectedCreaturePrefsKey = SelectedCreatureUtility.SelectedCreaturePrefsKey;

        public static SpawnManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private bool randomizePlayerSpawnPoint = true;
        [SerializeField] private bool avoidRepeatSpawnPoint = true;
        [SerializeField] private CreatureProfile[] selectablePlayerProfiles;
        [SerializeField] private string selectedCreatureId;
        [SerializeField] private bool spawnPlayerOnStart = true;
        [SerializeField] private bool autoRespawnPlayer = true;
        [SerializeField, Min(0f)] private float playerRespawnDelay = 3f;

        [Header("AI Zones")]
        [SerializeField] private SpawnZone[] spawnZones;
        [SerializeField] private bool autoCollectChildZones = true;

        public CoreCreature Player { get; private set; }
        public CreatureProfile SelectedPlayerProfile { get; private set; }
        public Transform LastPlayerSpawnPoint { get; private set; }

        private Coroutine playerRespawnRoutine;
        private int lastPlayerSpawnPointIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate SpawnManager detected. Destroying the newer instance.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveZones();
            ResolveSelectedProfile();
        }

        private void OnEnable()
        {
            GameEventBus.CreatureDied += OnCreatureDied;
        }

        private void OnDisable()
        {
            GameEventBus.CreatureDied -= OnCreatureDied;
        }

        private void Start()
        {
            if (spawnPlayerOnStart)
                SpawnPlayer();
            else
                InitializeZones();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public CreatureProfile[] GetSelectablePlayerProfiles()
        {
            return ResolveCreatureCatalog();
        }

        public void SelectPlayerProfile(CreatureProfile profile, bool persistSelection = true)
        {
            if (profile == null)
                return;

            SelectedPlayerProfile = profile;
            selectedCreatureId = profile.creatureId;

            if (persistSelection)
            {
                PlayerPrefs.SetString(SelectedCreaturePrefsKey, selectedCreatureId);
                PlayerPrefs.Save();
            }
        }

        public void SelectPlayerProfileById(string creatureId, bool persistSelection = true)
        {
            CreatureProfile profile = FindProfileById(creatureId);
            if (profile != null)
            {
                SelectPlayerProfile(profile, persistSelection);
                return;
            }

            Debug.LogWarning($"SpawnManager could not find a player CreatureProfile with id '{creatureId}'.", this);
        }

        public void SelectPlayerProfileByIndex(int index, bool persistSelection = true)
        {
            CreatureProfile[] catalog = ResolveCreatureCatalog();
            if (index < 0 || index >= catalog.Length)
                return;

            SelectPlayerProfile(catalog[index], persistSelection);
        }

        public CoreCreature SpawnPlayer(CreatureProfile profileOverride = null)
        {
            return SpawnPlayerAt(null, profileOverride);
        }

        public CoreCreature SpawnPlayerAt(Transform spawnPoint, CreatureProfile profileOverride = null)
        {
            CreatureProfile profile = profileOverride != null ? profileOverride : SelectedPlayerProfile;
            if (profile == null)
            {
                Debug.LogError("SpawnManager cannot spawn the player without a CreatureProfile.", this);
                return null;
            }

            if (profile.prefab == null)
            {
                Debug.LogError($"CreatureProfile '{profile.name}' has no prefab assigned.", profile);
                return null;
            }

            if (Player != null)
                DespawnPlayer();

            Transform chosenSpawnPoint = spawnPoint != null ? spawnPoint : ResolvePlayerSpawnPoint();
            LastPlayerSpawnPoint = chosenSpawnPoint;

            Vector3 position = chosenSpawnPoint != null ? chosenSpawnPoint.position : transform.position;
            Quaternion rotation = chosenSpawnPoint != null ? chosenSpawnPoint.rotation : Quaternion.identity;

            Player = CreatureSpawner.Spawn(
                profile,
                position,
                rotation,
                TeamType.Player,
                enableAI: false);

            if (Player == null)
                return null;

            Player.name = $"Player_{profile.displayName}";
            EnsurePlayerInput(Player);
            InitializeZones();
            return Player;
        }

        public void DespawnPlayer()
        {
            if (Player == null)
                return;

            CreatureSpawner.Despawn(Player, Player.Profile, usePool: false);
            Player = null;
            InitializeZones();
        }

        public void RespawnPlayer()
        {
            if (playerRespawnRoutine != null)
                StopCoroutine(playerRespawnRoutine);

            playerRespawnRoutine = StartCoroutine(RespawnPlayerRoutine());
        }

        private IEnumerator RespawnPlayerRoutine()
        {
            CoreCreature deadPlayer = Player;
            Player = null;

            float waitStart = Time.time;
            if (deadPlayer != null && deadPlayer.Animation != null)
            {
                while (!deadPlayer.Animation.IsDeathAnimationComplete
                       && Time.time - waitStart < deadPlayer.Animation.DeathAnimationMaxWait)
                {
                    yield return null;
                }
            }

            float elapsed = Time.time - waitStart;
            if (playerRespawnDelay > elapsed)
                yield return new WaitForSeconds(playerRespawnDelay - elapsed);

            if (deadPlayer != null)
                CreatureSpawner.Despawn(deadPlayer, deadPlayer.Profile, usePool: false);

            SpawnPlayer();
            playerRespawnRoutine = null;
        }

        private void OnCreatureDied(CoreCreature creature, GameObject source)
        {
            if (creature != Player || !autoRespawnPlayer)
                return;

            RespawnPlayer();
        }

        private void ResolveSelectedProfile()
        {
            string savedId = PlayerPrefs.GetString(SelectedCreaturePrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedId))
                selectedCreatureId = savedId;

            if (!string.IsNullOrWhiteSpace(selectedCreatureId))
            {
                SelectPlayerProfileById(selectedCreatureId, persistSelection: false);
                if (SelectedPlayerProfile != null)
                    return;
            }

            CreatureProfile[] catalog = ResolveCreatureCatalog();
            if (catalog.Length > 0)
                SelectPlayerProfile(catalog[0], persistSelection: false);
        }

        private CreatureProfile[] ResolveCreatureCatalog()
        {
            if (selectablePlayerProfiles != null && selectablePlayerProfiles.Length > 0)
                return selectablePlayerProfiles;

            if (CreatureRegistry.Instance != null)
                return CreatureRegistry.Instance.Creatures;

            return System.Array.Empty<CreatureProfile>();
        }

        private CreatureProfile FindProfileById(string creatureId)
        {
            if (string.IsNullOrWhiteSpace(creatureId))
                return null;

            if (selectablePlayerProfiles != null)
            {
                for (int i = 0; i < selectablePlayerProfiles.Length; i++)
                {
                    CreatureProfile profile = selectablePlayerProfiles[i];
                    if (profile != null && profile.creatureId == creatureId)
                        return profile;
                }
            }

            return CreatureRegistry.Instance != null
                ? CreatureRegistry.Instance.FindById(creatureId)
                : null;
        }

        private void ResolveZones()
        {
            if (!autoCollectChildZones && spawnZones != null && spawnZones.Length > 0)
                return;

            spawnZones = GetComponentsInChildren<SpawnZone>(includeInactive: true);
        }

        private void InitializeZones()
        {
            if (spawnZones == null)
                return;

            Transform playerTransform = Player != null ? Player.transform : null;
            for (int i = 0; i < spawnZones.Length; i++)
            {
                if (spawnZones[i] == null)
                    continue;

                spawnZones[i].Initialize(playerTransform);
            }
        }

        private Transform ResolvePlayerSpawnPoint()
        {
            Transform[] points = GetAvailablePlayerSpawnPoints();
            if (points.Length == 0)
                return null;

            if (!randomizePlayerSpawnPoint || points.Length == 1)
            {
                lastPlayerSpawnPointIndex = 0;
                return points[0];
            }

            int index = Random.Range(0, points.Length);

            if (avoidRepeatSpawnPoint && points.Length > 1 && index == lastPlayerSpawnPointIndex)
                index = (index + 1) % points.Length;

            lastPlayerSpawnPointIndex = index;
            return points[index];
        }

        private Transform[] GetAvailablePlayerSpawnPoints()
        {
            if (playerSpawnPoints != null && playerSpawnPoints.Length > 0)
            {
                int count = 0;
                for (int i = 0; i < playerSpawnPoints.Length; i++)
                {
                    if (playerSpawnPoints[i] != null)
                        count++;
                }

                if (count == 0)
                    return System.Array.Empty<Transform>();

                Transform[] filtered = new Transform[count];
                int writeIndex = 0;
                for (int i = 0; i < playerSpawnPoints.Length; i++)
                {
                    if (playerSpawnPoints[i] == null)
                        continue;

                    filtered[writeIndex++] = playerSpawnPoints[i];
                }

                return filtered;
            }

            if (playerSpawnPoint != null)
                return new[] { playerSpawnPoint };

            return System.Array.Empty<Transform>();
        }

        private static void EnsurePlayerInput(CoreCreature player)
        {
            if (player == null)
                return;

            if (!player.TryGetComponent<DinoPlayerInputNew>(out _))
                player.gameObject.AddComponent<DinoPlayerInputNew>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform[] points = GetAvailablePlayerSpawnPoints();
            if (points.Length == 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position, 1f);
                return;
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null)
                    continue;

                bool isLast = LastPlayerSpawnPoint == points[i];
                Gizmos.color = isLast ? Color.yellow : Color.green;
                Gizmos.DrawSphere(points[i].position, 1f);
                Gizmos.DrawLine(points[i].position, points[i].position + points[i].forward * 2f);
            }
        }
#endif
    }
}

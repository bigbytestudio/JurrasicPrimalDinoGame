using System.Collections.Generic;
using UnityEngine;
using DinoGame.AI;
using DinoGame.Components;
using DinoGame.Core;
using DinoGame.Data;
using CoreCreature = DinoGame.Core.Creature;
using DinoGame.Systems;

namespace DinoGame.Spawn
{
    [System.Serializable]
    public struct SpawnSlotDefinition
    {
        [Tooltip("World position and rotation for this spawn. Falls back to the zone origin when empty.")]
        public Transform spawnPoint;

        [Tooltip("Optional per-slot creature. When empty, a random profile from the zone pool is used.")]
        public CreatureProfile profileOverride;
    }

    /// <summary>
    /// Spatial AI spawn zone. CoreCreatures are only spawned and simulated while the player is nearby.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnZone : MonoBehaviour, IZonePatrolProvider
    {
        [Header("Activation")]
        [SerializeField, Min(1f)] private float activationRadius = 80f;
        [SerializeField, Min(1f)] private float deactivationRadius = 100f;
        [SerializeField, Min(0.1f)] private float checkInterval = 0.5f;
        [SerializeField] private bool keepActiveWhileEngaged = true;
        [SerializeField] private bool keepActiveWhileAlive = true;

        [Header("Auto Patrol")]
        [SerializeField] private bool generatePatrolPoints = true;
        [SerializeField, Min(3)] private int patrolPointCount = 8;
        [SerializeField, Min(5f)] private float patrolAreaRadius = 35f;
        [SerializeField, Min(0.1f)] private float patrolPointReachRadius = 1.5f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Creatures")]
        [SerializeField] private CreatureProfile[] creatureProfiles;
        [SerializeField] private SpawnSlotDefinition[] spawnSlots;
        [SerializeField, Min(0)] private int maxAlive = 8;
        [SerializeField, Min(0f)] private float respawnDelay = 8f;
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private bool despawnOnDeactivate = false;

        private readonly List<SpawnedEntry> alive = new();
        private Vector3[] zonePatrolPoints;
        private Transform playerTransform;
        private float nextCheckTime;
        private float nextRespawnTime;
        private bool isActive;

        public bool HasPatrolPoints => zonePatrolPoints != null && zonePatrolPoints.Length > 0;
        public int PatrolPointCount => zonePatrolPoints != null ? zonePatrolPoints.Length : 0;
        public float PointReachRadius => patrolPointReachRadius;

        private struct SpawnedEntry
        {
            public CoreCreature creature;
            public CreatureProfile profile;
            public int slotIndex;
        }

        public bool IsActive => isActive;
        public float ActivationRadius => activationRadius;
        public float DeactivationRadius => deactivationRadius;

        public void Initialize(Transform player)
        {
            playerTransform = player;
            nextCheckTime = 0f;

            if (playerTransform != null)
                EvaluateActivation(force: true);
        }

        public void SetPlayer(Transform player) => playerTransform = player;

        private void Update()
        {
            if (playerTransform == null)
                return;

            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + checkInterval;
                EvaluateActivation(force: false);
            }

            if (!isActive)
                return;

            CleanupDeadCreatures();
            TryRespawn();
        }

        private void EvaluateActivation(bool force)
        {
            if (playerTransform == null)
            {
                if (isActive && !ShouldKeepZoneActive())
                    Deactivate();

                return;
            }

            float sqrDistance = (transform.position - playerTransform.position).sqrMagnitude;
            float activationSqr = activationRadius * activationRadius;
            float deactivationSqr = deactivationRadius * deactivationRadius;

            if (!isActive && (force ? sqrDistance <= activationSqr : sqrDistance <= activationSqr))
                Activate();
            else if (isActive && sqrDistance > deactivationSqr && !ShouldKeepZoneActive())
                Deactivate();
        }

        public Vector3 GetPatrolPoint(int index)
        {
            if (!HasPatrolPoints)
                return transform.position;

            index = Mathf.Clamp(index, 0, zonePatrolPoints.Length - 1);
            return zonePatrolPoints[index];
        }

        public int GetRandomPatrolIndex(int excludeIndex = -1)
        {
            if (!HasPatrolPoints)
                return 0;

            if (zonePatrolPoints.Length == 1)
                return 0;

            int index = Random.Range(0, zonePatrolPoints.Length);
            if (index == excludeIndex)
                index = (index + 1) % zonePatrolPoints.Length;

            return index;
        }

        private void CleanupDeadCreatures()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                SpawnedEntry entry = alive[i];
                CoreCreature creature = entry.creature;

                if (creature == null)
                {
                    alive.RemoveAt(i);
                    continue;
                }

                if (creature.IsAlive)
                    continue;

                AnimationComponent animation = creature.Animation;
                if (animation != null && !animation.IsDeathAnimationComplete)
                    continue;

                CreatureSpawner.Despawn(creature, entry.profile, useObjectPooling);
                alive.RemoveAt(i);
            }
        }

        private bool ShouldKeepZoneActive()
        {
            if (!keepActiveWhileAlive && !keepActiveWhileEngaged)
                return false;

            for (int i = 0; i < alive.Count; i++)
            {
                CoreCreature creature = alive[i].creature;
                if (creature == null)
                    continue;

                if (!creature.IsAlive)
                {
                    AnimationComponent animation = creature.Animation;
                    if (animation != null && !animation.IsDeathAnimationComplete)
                        return true;

                    continue;
                }

                if (keepActiveWhileAlive)
                    return true;

                AIComponent ai = creature.AI;
                if (ai != null && ai.IsEngaged)
                    return true;
            }

            return false;
        }

        private void Activate()
        {
            if (isActive)
                return;

            isActive = true;
            EnsurePatrolPoints();

            if (alive.Count > 0)
            {
                for (int i = 0; i < alive.Count; i++)
                {
                    CoreCreature creature = alive[i].creature;
                    if (creature == null)
                        continue;

                    if (!creature.gameObject.activeSelf)
                        creature.gameObject.SetActive(true);

                    ConfigureAIPatrol(creature);
                }

                return;
            }

            PopulateZone();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (deactivationRadius < activationRadius)
                deactivationRadius = activationRadius;
        }
#endif

        private void Deactivate()
        {
            if (!isActive)
                return;

            isActive = false;

            if (despawnOnDeactivate)
            {
                for (int i = alive.Count - 1; i >= 0; i--)
                {
                    SpawnedEntry entry = alive[i];
                    CreatureSpawner.Despawn(entry.creature, entry.profile, useObjectPooling);
                }

                alive.Clear();
                return;
            }

            for (int i = 0; i < alive.Count; i++)
            {
                SpawnedEntry entry = alive[i];
                if (entry.creature != null)
                    entry.creature.gameObject.SetActive(false);
            }
        }

        private void PopulateZone()
        {
            if (creatureProfiles == null || creatureProfiles.Length == 0)
                return;

            int targetCount = ResolveTargetCount();
            int spawned = 0;

            if (spawnSlots != null && spawnSlots.Length > 0)
            {
                for (int i = 0; i < spawnSlots.Length && spawned < targetCount; i++)
                {
                    if (TrySpawnAtSlot(i))
                        spawned++;
                }
            }
            else
            {
                while (spawned < targetCount)
                {
                    if (!TrySpawnAtSlot(-1))
                        break;

                    spawned++;
                }
            }
        }

        private void TryRespawn()
        {
            if (maxAlive <= 0 || respawnDelay <= 0f)
                return;

            int targetCount = ResolveTargetCount();
            if (alive.Count >= targetCount || Time.time < nextRespawnTime)
                return;

            bool spawned = false;

            if (spawnSlots != null && spawnSlots.Length > 0)
            {
                for (int i = 0; i < spawnSlots.Length && alive.Count < targetCount; i++)
                {
                    if (IsSlotOccupied(i))
                        continue;

                    if (TrySpawnAtSlot(i))
                        spawned = true;
                }
            }
            else if (alive.Count < targetCount)
            {
                spawned = TrySpawnAtSlot(-1);
            }

            if (spawned)
                nextRespawnTime = Time.time + respawnDelay;
        }

        private int ResolveTargetCount()
        {
            if (maxAlive > 0)
                return maxAlive;

            return spawnSlots != null && spawnSlots.Length > 0 ? spawnSlots.Length : 1;
        }

        private bool IsSlotOccupied(int slotIndex)
        {
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i].slotIndex == slotIndex)
                    return true;
            }

            return false;
        }

        private bool TrySpawnAtSlot(int slotIndex)
        {
            CreatureProfile profile = ResolveProfile(slotIndex);
            if (profile == null || profile.prefab == null)
                return false;

            Vector3 position;
            Quaternion rotation;
            ResolvePose(slotIndex, out position, out rotation);

            CoreCreature creature = CreatureSpawner.Spawn(
                profile,
                position,
                rotation,
                profile.defaultTeam,
                enableAI: true);

            if (creature == null)
                return false;

            alive.Add(new SpawnedEntry
            {
                creature = creature,
                profile = profile,
                slotIndex = slotIndex
            });

            ConfigureAIPatrol(creature);
            return true;
        }

        private void ConfigureAIPatrol(CoreCreature creature)
        {
            if (creature == null || !creature.TryGetComponent(out AIComponent ai))
                return;

            EnsurePatrolPoints();
            ai.SetZonePatrol(generatePatrolPoints ? this : null);
        }

        private void EnsurePatrolPoints()
        {
            if (!generatePatrolPoints)
                return;

            if (zonePatrolPoints != null && zonePatrolPoints.Length == patrolPointCount)
                return;

            zonePatrolPoints = new Vector3[patrolPointCount];
            for (int i = 0; i < patrolPointCount; i++)
                zonePatrolPoints[i] = GeneratePatrolPoint();
        }

        private Vector3 GeneratePatrolPoint()
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 circle = Random.insideUnitCircle;
                if (circle.sqrMagnitude < 0.01f)
                    circle = Random.insideUnitCircle.normalized;

                float radius = Random.Range(patrolAreaRadius * 0.35f, patrolAreaRadius);
                Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y) * radius;

                if (Physics.Raycast(candidate + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 80f, groundMask, QueryTriggerInteraction.Ignore))
                    return hit.point;
            }

            return transform.position;
        }

        private CreatureProfile ResolveProfile(int slotIndex)
        {
            if (slotIndex >= 0 && spawnSlots != null && slotIndex < spawnSlots.Length)
            {
                CreatureProfile slotProfile = spawnSlots[slotIndex].profileOverride;
                if (slotProfile != null)
                    return slotProfile;
            }

            if (creatureProfiles == null || creatureProfiles.Length == 0)
                return null;

            return creatureProfiles[Random.Range(0, creatureProfiles.Length)];
        }

        private void ResolvePose(int slotIndex, out Vector3 position, out Quaternion rotation)
        {
            if (slotIndex >= 0 && spawnSlots != null && slotIndex < spawnSlots.Length && spawnSlots[slotIndex].spawnPoint != null)
            {
                Transform point = spawnSlots[slotIndex].spawnPoint;
                position = point.position;
                rotation = point.rotation;
                return;
            }

            position = transform.position;
            rotation = transform.rotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, activationRadius);
            Gizmos.color = new Color(0.9f, 0.3f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, deactivationRadius);

            if (zonePatrolPoints != null && zonePatrolPoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < zonePatrolPoints.Length; i++)
                    Gizmos.DrawSphere(zonePatrolPoints[i], 0.6f);
            }

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, patrolAreaRadius);

            if (spawnSlots == null)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < spawnSlots.Length; i++)
            {
                Transform point = spawnSlots[i].spawnPoint;
                if (point == null)
                    continue;

                Gizmos.DrawSphere(point.position, 0.75f);
                Gizmos.DrawLine(transform.position, point.position);
            }
        }
#endif
    }
}

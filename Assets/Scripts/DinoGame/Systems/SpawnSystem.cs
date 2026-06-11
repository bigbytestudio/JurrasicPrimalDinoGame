using System.Collections.Generic;
using UnityEngine;
using DinoGame.Core;
using DinoGame.Data;

namespace DinoGame.Systems
{
    public sealed class SpawnSystem : MonoBehaviour
    {
        [SerializeField] private SpawnConfig config;
        private readonly List<Creature> alive = new();
        private float nextSpawnTime;

        private void Update()
        {
            if (config == null || config.creatures == null || config.creatures.Length == 0) return;
            alive.RemoveAll(c => c == null || !c.IsAlive);
            if (alive.Count >= config.maxAlive || Time.time < nextSpawnTime) return;
            SpawnRandom();
            nextSpawnTime = Time.time + config.respawnDelay;
        }

        public Creature Spawn(CreatureProfile profile, Vector3 position, Quaternion rotation)
        {
            Creature creature = CreatureSpawner.Spawn(profile, position, rotation);
            if (creature != null)
                alive.Add(creature);

            return creature;
        }

        private void SpawnRandom()
        {
            CreatureProfile profile = config.creatures[Random.Range(0, config.creatures.Length)];
            Transform point = null;
            if (config.spawnPoints != null && config.spawnPoints.Length > 0)
                point = config.spawnPoints[Random.Range(0, config.spawnPoints.Length)];
            Vector3 pos = point != null ? point.position : transform.position;
            Quaternion rot = point != null ? point.rotation : Quaternion.identity;
            Spawn(profile, pos, rot);
        }
    }
}

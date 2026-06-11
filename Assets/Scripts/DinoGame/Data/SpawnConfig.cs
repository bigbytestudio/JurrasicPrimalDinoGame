using UnityEngine;

namespace DinoGame.Data
{
    [CreateAssetMenu(menuName = "Dino Game/Data/Spawn Config", fileName = "SpawnConfig")]
    public sealed class SpawnConfig : ScriptableObject
    {
        public CreatureProfile[] creatures;
        public Transform[] spawnPoints;
        [Min(0)] public int maxAlive = 12;
        [Min(0f)] public float respawnDelay = 8f;
    }
}

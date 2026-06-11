using UnityEngine;
using DinoGame.Core;
using DinoGame.Strategies.Movement;
using DinoGame.Strategies.Attack;
using DinoGame.Strategies.AI;

namespace DinoGame.Data
{
    [CreateAssetMenu(menuName = "Dino Game/Data/Creature Profile", fileName = "CreatureProfile")]
    public sealed class CreatureProfile : ScriptableObject
    {
        [Header("Identity")]
        public string creatureId = "creature";
        public string displayName = "Creature";
        public TeamType defaultTeam = TeamType.Neutral;
        public GameObject prefab;

        [Header("Stats")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float stamina = 100f;
        [Min(0f)] public float moveSpeed = 3.5f;
        [Min(0f)] public float sprintSpeed = 6f;
        [Min(0f)] public float turnSpeed = 540f;
        [Min(0f)] public float detectionRadius = 18f;
        [Range(1f, 360f)] public float fieldOfView = 140f;

        [Header("Behavior")]
        public MovementStrategy movementStrategy;
        public AttackStrategy[] attackStrategies;
        public AIStrategy aiStrategy;
        public PerceptionStrategy perceptionStrategy;
    }
}

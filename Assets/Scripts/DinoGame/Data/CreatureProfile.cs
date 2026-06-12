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
        public string creatureCode;
        public TeamType defaultTeam = TeamType.Neutral;
        public GameObject prefab;

        [Header("Unlock")]
        public bool unlockedByDefault = true;
        [Min(0)] public int bonePurchaseCost = 500;

        [Header("Menu Display")]
        public Sprite previewIcon;
        [Min(0)] public int infamyLevel = 1;
        public string infamyTierLabel = "HARMLESS";
        [Range(0, 100)] public int attackPercent = 50;
        [Range(0, 100)] public int defensePercent = 50;
        [Range(0, 100)] public int staminaPercent = 50;
        [Min(0)] public int growthLevel = 1;
        [Min(1)] public int maxGrowthLevel = 4;
        [Min(0)] public int growthUpgradeDnaCost = 15;
        public string growthStageLabel = "DEVELOPMENT STAGE";

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

        public string GetCreatureCode()
        {
            return string.IsNullOrWhiteSpace(creatureCode) ? creatureId.ToUpperInvariant() : creatureCode;
        }
    }
}

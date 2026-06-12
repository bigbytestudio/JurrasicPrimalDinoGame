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
        [Tooltip("Small portrait used on the main menu selected dino icon and dino selection stats panel.")]
        public Sprite profilePortrait;
        [Tooltip("Portrait used on dino selection cards. Falls back to profile portrait when empty.")]
        public Sprite cardPortrait;
        [Tooltip("Card image size in UI units. Leave at 0 to keep the card layout size.")]
        public Vector2 cardPortraitSize;
        [Tooltip("Legacy menu icon. Used only when profile portrait is not assigned.")]
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

        [Header("Growth Panel")]
        public string dietTypeLabel = "Herbivore";
        public float sizeMeters = 4.9f;
        public float[] sizeMetersPerStage = { 2.5f, 4.9f, 6.5f, 8f };
        [Range(0f, 1f)] public float growthHealthFill = 0.75f;
        [Range(0f, 1f)] public float growthDamageFill = 0.65f;
        [Range(0f, 1f)] public float growthSpeedFill = 0.8f;
        [Range(0f, 1f)] public float growthSwimFill = 0.5f;
        [Range(0f, 1f)] public float lifeSleepFill = 0.7f;
        [Range(0f, 1f)] public float lifeWaterFill = 0.6f;
        [Range(0f, 1f)] public float lifeHungerFill = 0.55f;

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

        public Sprite GetProfilePortrait()
        {
            if (profilePortrait != null)
                return profilePortrait;

            return previewIcon;
        }

        public Sprite GetCardPortrait()
        {
            if (cardPortrait != null)
                return cardPortrait;

            return GetProfilePortrait();
        }

        public bool TryGetCardPortraitSize(out Vector2 size)
        {
            size = cardPortraitSize;
            return size.x > 0f && size.y > 0f;
        }

        public float GetSizeMetersForGrowthLevel(int growthLevel)
        {
            int stageIndex = Mathf.Max(0, growthLevel - 1);
            if (sizeMetersPerStage != null && stageIndex < sizeMetersPerStage.Length)
                return sizeMetersPerStage[stageIndex];

            return sizeMeters;
        }
    }
}

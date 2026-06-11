using UnityEngine;

namespace DinoGame.Data
{
    [CreateAssetMenu(menuName = "Dino Game/Data/AI Config", fileName = "AIConfig")]
    public sealed class AIConfig : ScriptableObject
    {
        [Min(0f)] public float thinkInterval = 0.2f;
        [Min(0f)] public float attackDistance = 2.5f;
        [Min(0f)] public float combatStandOffBuffer = 0.5f;
        [Min(0f)] public float chaseDistance = 22f;
        [Min(0f)] public float chaseLoseBuffer = 8f;
        [Min(0f)] public float maxChaseTime = 12f;
        [Min(0f)] public float loseSightGraceTime = 4f;
        [Min(0f)] public float growlSkipSpeed = 6f;
        [Min(0f)] public float proximityDetectionRadius = 14f;
        [Min(0f)] public float movingTargetSenseSpeed = 1.5f;
        [Range(0.05f, 0.95f)] public float fleeHealthPercent = 0.2f;
        [Range(0.05f, 0.95f)] public float woundedHealthPercent = 0.4f;
        [Range(0.1f, 1f)] public float woundedMinSpeedMultiplier = 0.32f;
        public bool attacksDifferentTeams = true;

        [Header("Patrol")]
        [Min(0f)] public float patrolWaitTime = 2f;
        [Min(0.1f)] public float patrolPointReachDistance = 1.5f;
        [Range(0f, 1f)] public float patrolRunChance = 0.35f;
    }
}

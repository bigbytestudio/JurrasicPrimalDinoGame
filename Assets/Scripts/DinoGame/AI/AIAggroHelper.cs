using UnityEngine;
using DinoGame.Combat;
using DinoGame.Components;
using DinoGame.Core;
using DinoGame.Data;
using DinoGame.Interfaces;

namespace DinoGame.AI
{
    internal static class AIAggroHelper
    {
        public static float GetLoseChaseDistance(AIConfig config)
        {
            float chaseDistance = config != null ? config.chaseDistance : 22f;
            float buffer = config != null ? config.chaseLoseBuffer : 8f;
            return chaseDistance + buffer;
        }

        public static float GetScanRadius(AIConfig config, float detectionRadius, AIBehaviorState state)
        {
            if (state == null || !state.HasAggro)
                return detectionRadius;

            return Mathf.Max(detectionRadius, GetLoseChaseDistance(config));
        }

        public static float GetTargetMoveSpeed(ITargetable target)
        {
            if (target?.TargetTransform == null)
                return 0f;

            Creature creature = target.TargetTransform.GetComponentInParent<Creature>();
            return creature?.Movement != null ? creature.Movement.CurrentMoveSpeed : 0f;
        }

        public static bool ShouldGrowlBeforeChase(Creature owner, AIComponent ai, ITargetable target, float attackDistance, float distance)
        {
            if (owner == null || ai == null || target == null)
                return false;

            if (distance <= attackDistance + 2.5f)
                return false;

            float skipSpeed = ai.Config != null ? ai.Config.growlSkipSpeed : 6f;
            return GetTargetMoveSpeed(target) < skipSpeed;
        }

        public static bool ShouldAbandonChase(Creature owner, AIComponent ai, ITargetable target, AIBehaviorState state)
        {
            if (owner == null || ai == null || target == null || state == null || !target.IsAlive)
                return true;

            AIConfig config = ai.Config;
            float distance = CombatSpacing.GetFlatDistance(owner, target);
            float chaseDistance = config != null ? config.chaseDistance : 22f;

            if (!state.HasAggro)
                return distance > chaseDistance;

            float loseDistance = GetLoseChaseDistance(config);
            if (distance <= loseDistance)
            {
                if (owner.CanSee(target))
                    state.MarkTargetSeen();

                return false;
            }

            float grace = config != null ? config.loseSightGraceTime : 4f;
            if (Time.time - state.LastSeenTargetTime <= grace)
                return false;

            return !owner.CanSee(target);
        }

        public static void TrackVisibleTarget(Creature owner, AIComponent ai, ITargetable target, AIBehaviorState state)
        {
            if (owner == null || ai == null || target == null || state == null || !target.IsAlive)
                return;

            float loseDistance = GetLoseChaseDistance(ai.Config);
            if (CombatSpacing.GetFlatDistance(owner, target) > loseDistance)
                return;

            if (owner.CanSee(target))
                state.MarkTargetSeen();
        }
    }
}

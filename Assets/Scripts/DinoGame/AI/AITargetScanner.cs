using UnityEngine;
using DinoGame.Combat;
using DinoGame.Core;
using DinoGame.Components;
using DinoGame.Data;
using DinoGame.Interfaces;

namespace DinoGame.AI
{
    internal static class AITargetScanner
    {
        public static ITargetable FindBestTarget(
            Creature owner,
            AIComponent ai,
            LayerMask mask,
            ITargetable current,
            float detectionRadius,
            AIBehaviorState state)
        {
            AIConfig config = ai != null ? ai.Config : null;
            float scanRadius = AIAggroHelper.GetScanRadius(config, detectionRadius, state);
            float validateRange = state != null && state.HasAggro
                ? Mathf.Max(detectionRadius, config != null ? config.chaseDistance : detectionRadius)
                : detectionRadius;

            if (IsValidTarget(owner, ai, current, validateRange, state))
                return current;

            if (CanRetainChaseTarget(owner, ai, current, state))
                return current;

            Collider[] hits = Physics.OverlapSphere(
                owner.transform.position,
                scanRadius,
                mask,
                CombatPhysics.TargetQuery);

            ITargetable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TargetableResolver.TryResolve(hits[i], out ITargetable candidate))
                    continue;

                if (!IsValidTarget(owner, ai, candidate, detectionRadius, state))
                    continue;

                float sqr = (candidate.TargetTransform.position - owner.transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    best = candidate;
                    bestSqr = sqr;
                }
            }

            return best;
        }

        public static bool IsValidTarget(Creature owner, AIComponent ai, ITargetable target, float maxRange, AIBehaviorState state)
        {
            if (owner == null || ai == null || target == null || !target.IsAlive)
                return false;

            if (target.TargetTransform == owner.transform)
                return false;

            bool attackDifferentTeams = ai.Config == null || ai.Config.attacksDifferentTeams;
            if (attackDifferentTeams && target.TeamId == owner.TeamId)
                return false;

            float sqr = (target.TargetTransform.position - owner.transform.position).sqrMagnitude;
            if (sqr > maxRange * maxRange)
                return false;

            if (!owner.CanSee(target))
                return false;

            state?.MarkTargetSeen();
            return true;
        }

        private static bool CanRetainChaseTarget(Creature owner, AIComponent ai, ITargetable target, AIBehaviorState state)
        {
            if (owner == null || ai == null || target == null || state == null || !target.IsAlive)
                return false;

            if (target.TargetTransform == owner.transform)
                return false;

            if (!state.HasAggro
                && state.Current is not (AIState.Chase or AIState.Attack or AIState.Growl))
                return false;

            return !AIAggroHelper.ShouldAbandonChase(owner, ai, target, state);
        }
    }
}

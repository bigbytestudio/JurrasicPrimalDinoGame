using UnityEngine;
using DinoGame.AI;
using DinoGame.Combat;
using DinoGame.Core;
using DinoGame.Components;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.AI
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/AI/Simple Chase Attack", fileName = "SimpleChaseAttackAI")]
    public sealed class SimpleDecisionStrategy : AIStrategy
    {
        [SerializeField] private LayerMask targetMask = ~0;

        [Header("Chase")]
        [SerializeField] private float sprintDistance = 5f;
        [SerializeField] private float stopDistanceBuffer = 0.25f;

        public override ITargetable Tick(Creature owner, AIComponent ai, ITargetable currentTarget)
        {
            if (owner == null || ai == null)
                return null;

            ITargetable target = IsValid(owner, ai, currentTarget)
                ? currentTarget
                : FindTarget(owner, ai);

            if (target == null)
            {
                owner.Sprint(false);
                owner.Stop();
                return null;
            }

            Vector3 toTarget = target.TargetTransform.position - owner.transform.position;
            toTarget.y = 0f;

            float attackDistance = owner.Combat != null ? owner.Combat.GetMeleeRange() : 2.2f;
            float separationBuffer = ai.Config != null ? ai.Config.combatStandOffBuffer : stopDistanceBuffer;

            if (AICombatMovement.HandleCombatRange(owner, ai, target, attackDistance, separationBuffer))
                return target;

            Vector3 direction = toTarget.sqrMagnitude > 0.001f
                ? toTarget.normalized
                : Vector3.zero;

            bool shouldSprint = AICombatMovement.ShouldSprintToTarget(owner, target, sprintDistance, attackDistance);

            owner.Sprint(shouldSprint);
            owner.Move(direction);
            owner.Rotate(direction);

            return target;
        }

        private ITargetable FindTarget(Creature owner, AIComponent ai)
        {
            float radius = owner.Profile != null ? owner.Profile.detectionRadius : 18f;

            Collider[] hits = Physics.OverlapSphere(
                owner.transform.position,
                radius,
                targetMask,
                CombatPhysics.TargetQuery
            );

            ITargetable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TargetableResolver.TryResolve(hits[i], out ITargetable candidate))
                    continue;

                if (!IsValid(owner, ai, candidate))
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

        private static bool IsValid(Creature owner, AIComponent ai, ITargetable target)
        {
            if (owner == null || ai == null || target == null)
                return false;

            if (!target.IsAlive)
                return false;

            if (target.TargetTransform == owner.transform)
                return false;

            bool attackDifferentTeams = ai.Config == null || ai.Config.attacksDifferentTeams;

            if (attackDifferentTeams && target.TeamId == owner.TeamId)
                return false;

            return owner.CanSee(target);
        }
    }
}
using UnityEngine;
using DinoGame.AI;
using DinoGame.Core;
using DinoGame.Components;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.AI
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/AI/Flee", fileName = "FleeAI")]
    public sealed class FleeAIStrategy : AIStrategy
    {
        [SerializeField] private LayerMask threatMask = ~0;

        [Header("Flee Settings")]
        [SerializeField] private bool alwaysSprint = true;
        [SerializeField] private float safeDistance = 20f;

        public override ITargetable Tick(Creature owner, AIComponent ai, ITargetable currentTarget)
        {
            if (owner == null || ai == null)
                return null;

            ITargetable threat = IsValidThreat(owner, ai, currentTarget)
                ? currentTarget
                : FindThreat(owner, ai);

            if (threat == null)
            {
                owner.Sprint(false);
                owner.Stop();
                return null;
            }

            Vector3 away = owner.transform.position - threat.TargetTransform.position;
            away.y = 0f;

            float distance = away.magnitude;

            if (distance >= safeDistance)
            {
                owner.Sprint(false);
                owner.Stop();
                return null;
            }

            Vector3 direction = away.sqrMagnitude > 0.001f
                ? away.normalized
                : -owner.transform.forward;

            owner.Sprint(alwaysSprint);
            owner.Move(direction);
            owner.Rotate(direction);

            return threat;
        }

        private ITargetable FindThreat(Creature owner, AIComponent ai)
        {
            float radius = owner.Profile != null ? owner.Profile.detectionRadius : 18f;

            Collider[] hits = Physics.OverlapSphere(
                owner.transform.position,
                radius,
                threatMask,
                CombatPhysics.TargetQuery
            );

            ITargetable closestThreat = null;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TargetableResolver.TryResolve(hits[i], out ITargetable target))
                    continue;

                if (!IsValidThreat(owner, ai, target))
                    continue;

                float sqrDistance = (target.TargetTransform.position - owner.transform.position).sqrMagnitude;

                if (sqrDistance < closestSqrDistance)
                {
                    closestThreat = target;
                    closestSqrDistance = sqrDistance;
                }
            }

            return closestThreat;
        }

        private static bool IsValidThreat(Creature owner, AIComponent ai, ITargetable target)
        {
            if (owner == null || ai == null || target == null)
                return false;

            if (!target.IsAlive)
                return false;

            if (target.TargetTransform == owner.transform)
                return false;

            if (target.TeamId == owner.TeamId)
                return false;

            return owner.CanSee(target);
        }
    }
}
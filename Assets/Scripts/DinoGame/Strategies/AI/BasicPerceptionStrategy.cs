using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.AI
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/AI/Basic Perception", fileName = "BasicPerception")]
    public sealed class BasicPerceptionStrategy : PerceptionStrategy
    {
        [SerializeField] private LayerMask obstacleMask = 0;
        [SerializeField] private bool useLineOfSight = true;

        public override bool CanSee(Creature owner, ITargetable target, float radius, float fieldOfView)
        {
            if (owner == null || target == null || !target.IsAlive) return false;
            Vector3 origin = owner.transform.position + Vector3.up;
            Vector3 destination = target.TargetTransform.position + Vector3.up;
            Vector3 toTarget = destination - origin;
            if (toTarget.sqrMagnitude > radius * radius) return false;
            if (Vector3.Angle(owner.transform.forward, toTarget.normalized) > fieldOfView * 0.5f) return false;
            if (!useLineOfSight) return true;
            return !Physics.Raycast(origin, toTarget.normalized, toTarget.magnitude, obstacleMask, QueryTriggerInteraction.Ignore);
        }
    }
}

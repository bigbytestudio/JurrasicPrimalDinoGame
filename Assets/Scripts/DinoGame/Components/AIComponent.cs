using UnityEngine;
using DinoGame.AI;
using DinoGame.Combat;
using DinoGame.Core;
using DinoGame.Data;
using DinoGame.Interfaces;
using DinoGame.Strategies.AI;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    public sealed class AIComponent : MonoBehaviour
    {
        [SerializeField] private AIConfig config;
        [SerializeField] private AIPatrolRoute patrolRoute;
        [SerializeField] private bool autoFindPatrolRoute = true;

        private Creature owner;
        private CreatureProfile profile;
        private float nextThinkTime;
        private ITargetable currentTarget;
        private IZonePatrolProvider zonePatrol;
        private readonly AIBehaviorState behaviorState = new();

        public ITargetable CurrentTarget => currentTarget;
        public AIConfig Config => config;
        public AIBehaviorState Behavior => behaviorState;
        public AIState CurrentState => behaviorState.Current;
        public AIPatrolRoute PatrolRoute => patrolRoute;
        public IZonePatrolProvider ZonePatrol => zonePatrol;

        public bool IsEngaged =>
            behaviorState.Current is AIState.Chase or AIState.Attack or AIState.Flee or AIState.Growl;

        public void Initialize(Creature creature, CreatureProfile creatureProfile)
        {
            owner = creature;
            profile = creatureProfile;

            if (autoFindPatrolRoute && patrolRoute == null)
                patrolRoute = GetComponent<AIPatrolRoute>() ?? GetComponentInChildren<AIPatrolRoute>();

            behaviorState.HomePosition = transform.position;
            behaviorState.HomeYaw = transform.eulerAngles.y;
            behaviorState.PatrolIndex = 0;
            behaviorState.WaitTimer = 0f;
            behaviorState.Current = AIState.Patrol;
            behaviorState.GrowlPlayedForCurrentTarget = false;
            behaviorState.ChaseTimer = 0f;
            behaviorState.PatrolUseRun = false;
            behaviorState.PatrolPaceRolled = false;
            behaviorState.ChaseSprint = false;
            behaviorState.LastSeenTargetTime = 0f;
            behaviorState.HasAggro = false;
        }

        public void SetZonePatrol(IZonePatrolProvider provider)
        {
            zonePatrol = provider;

            if (zonePatrol != null && zonePatrol.HasPatrolPoints)
                behaviorState.PatrolIndex = zonePatrol.GetRandomPatrolIndex();
        }

        public void EnforceMovementLock()
        {
            if (owner == null)
                return;

            float separationBuffer = config != null ? config.combatStandOffBuffer : 0.35f;

            if (currentTarget != null && currentTarget.IsAlive
                && CombatSpacing.IsOverlapping(owner, currentTarget, separationBuffer, out _))
            {
                owner.Sprint(false);
                owner.Move(CombatSpacing.GetBackOffDirection(owner, currentTarget));
                owner.Rotate(CombatSpacing.GetApproachDirection(owner, currentTarget));
                return;
            }

            bool lockMovement = owner.Animation != null
                && (owner.Animation.IsAttacking || owner.Animation.IsGrowling);

            if (!lockMovement)
                return;

            owner.Sprint(false);
            owner.Movement?.HaltForCombat();

            ITargetable focus = owner.Combat != null
                ? owner.Combat.ResolveAttackFocus()
                : currentTarget;

            if (focus != null)
                owner.FaceTarget(focus);
        }

        public void Tick(float deltaTime)
        {
            if (owner == null || !owner.IsAlive || profile == null || profile.aiStrategy == null)
                return;

            if (!enabled)
                return;

            EnforceMovementLock();

            bool animBlocked = owner.Animation != null
                && (owner.Animation.IsAttacking || owner.Animation.IsGrowling);

            if (behaviorState.HasAggro && currentTarget != null)
                AIAggroHelper.TrackVisibleTarget(owner, this, currentTarget, behaviorState);

            ApplyChaseMovement();

            float interval = config != null ? config.thinkInterval : 0.2f;

            if (animBlocked)
            {
                if (Time.time >= nextThinkTime)
                {
                    nextThinkTime = Time.time + interval;
                    RefreshAggroTarget();
                }

                return;
            }

            if (Time.time < nextThinkTime)
                return;

            nextThinkTime = Time.time + interval;
            currentTarget = profile.aiStrategy.Tick(owner, this, currentTarget);
        }

        private void RefreshAggroTarget()
        {
            if (!behaviorState.HasAggro || owner == null || profile == null)
                return;

            float detectionRadius = profile.detectionRadius;
            LayerMask mask = ~0;

            if (profile.aiStrategy is HunterAIStrategy hunter)
                mask = hunter.TargetMask;

            currentTarget = AITargetScanner.FindBestTarget(
                owner,
                this,
                mask,
                currentTarget,
                detectionRadius,
                behaviorState);
        }

        public bool CanSee(ITargetable target)
        {
            if (target == null || profile == null)
                return false;

            if (PassesProximityOrMotionSense(target))
                return true;

            if (profile.perceptionStrategy != null)
                return profile.perceptionStrategy.CanSee(owner, target, profile.detectionRadius, profile.fieldOfView);

            Vector3 toTarget = target.TargetTransform.position - transform.position;
            float detectionRadius = profile.detectionRadius;
            if (toTarget.sqrMagnitude > detectionRadius * detectionRadius)
                return false;

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            return angle <= profile.fieldOfView * 0.5f;
        }

        private bool PassesProximityOrMotionSense(ITargetable target)
        {
            Vector3 toTarget = target.TargetTransform.position - transform.position;
            float detectionRadius = profile.detectionRadius;
            float detectionSqr = detectionRadius * detectionRadius;
            if (toTarget.sqrMagnitude > detectionSqr)
                return false;

            float proximityRadius = config != null
                ? Mathf.Min(config.proximityDetectionRadius, detectionRadius)
                : detectionRadius * 0.65f;

            if (toTarget.sqrMagnitude <= proximityRadius * proximityRadius)
                return true;

            return IsTargetMoving(target);
        }

        private bool IsTargetMoving(ITargetable target)
        {
            if (target?.TargetTransform == null)
                return false;

            Creature creature = target.TargetTransform.GetComponentInParent<Creature>();
            if (creature == null)
                return false;

            float threshold = config != null ? config.movingTargetSenseSpeed : 1.5f;
            return creature.Movement != null && creature.Movement.CurrentMoveSpeed >= threshold;
        }

        private void ApplyChaseMovement()
        {
            if (owner == null || currentTarget == null || !currentTarget.IsAlive || !behaviorState.HasAggro)
                return;

            if (owner.Animation != null && (owner.Animation.IsAttacking || owner.Animation.IsGrowling))
                return;

            float attackRange = owner.Combat != null ? owner.Combat.GetMeleeRange() : 2.2f;
            float distance = CombatSpacing.GetFlatDistance(owner, currentTarget);

            if (distance <= attackRange * 1.15f)
                return;

            if (behaviorState.Current is AIState.Attack or AIState.Growl)
                behaviorState.Current = AIState.Chase;

            if (behaviorState.Current != AIState.Chase)
                return;

            Vector3 toTarget = CombatSpacing.GetFlatOffset(owner, currentTarget);
            if (toTarget.sqrMagnitude < 0.01f)
                return;

            if (!behaviorState.ChaseSprint)
            {
                behaviorState.ChaseSprint = !owner.IsWounded()
                    && distance > attackRange * 1.6f;
            }

            Vector3 direction = toTarget.normalized;
            owner.Sprint(behaviorState.ChaseSprint);
            owner.Move(direction);
            owner.Rotate(direction);
        }

        public void Dispose()
        {
            currentTarget = null;
            behaviorState.GrowlPlayedForCurrentTarget = false;
            behaviorState.ChaseTimer = 0f;
            behaviorState.PatrolUseRun = false;
            behaviorState.PatrolPaceRolled = false;
            behaviorState.ChaseSprint = false;
            behaviorState.LastSeenTargetTime = 0f;
            behaviorState.HasAggro = false;
        }
    }
}

using UnityEngine;
using DinoGame.AI;
using DinoGame.Combat;
using DinoGame.Core;
using DinoGame.Data;
using DinoGame.Components;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.AI
{
    /// <summary>
    /// Full predator AI: patrol, chase, attack with animations, optional growl and flee.
    /// Uses the same movement and animation systems as the player creature.
    /// </summary>
    [CreateAssetMenu(menuName = "Dino Game/Strategies/AI/Hunter Patrol Chase Attack", fileName = "HunterAI")]
    public sealed class HunterAIStrategy : AIStrategy
    {
        [SerializeField] private LayerMask targetMask = ~0;

        public LayerMask TargetMask => targetMask;

        [Header("Patrol")]
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField, Min(0.1f)] private float patrolPointReachDistance = 1.5f;
        [SerializeField, Min(0f)] private float wanderRadius = 12f;
        [SerializeField, Range(0f, 1f)] private float patrolRunChance = 0.35f;

        [Header("Chase")]
        [SerializeField, Min(0f)] private float sprintDistance = 8f;
        [SerializeField, Min(0f)] private float chaseLoseDistance = 24f;
        [SerializeField] private bool growlBeforeChase = true;

        [Header("Combat")]
        [SerializeField, Min(0f)] private float stopDistanceBuffer = 0.25f;

        [Header("Wounded")]
        [SerializeField] private bool retreatWhenLowHealth = true;
        [SerializeField, Range(0.05f, 0.95f)] private float retreatHealthPercent = 0.25f;
        [SerializeField, Min(0f)] private float retreatSafeDistance = 12f;

        public override ITargetable Tick(Creature owner, AIComponent ai, ITargetable currentTarget)
        {
            if (owner == null || ai == null)
                return null;

            AIBehaviorState state = ai.Behavior;
            AIConfig config = ai.Config;

            float detectionRadius = owner.Profile != null ? owner.Profile.detectionRadius : 18f;
            float chaseDistance = config != null ? config.chaseDistance : chaseLoseDistance;
            float maxChaseTime = config != null ? config.maxChaseTime : 12f;
            float attackDistance = ResolveMeleeRange(owner, config);
            float separationBuffer = config != null ? config.combatStandOffBuffer : stopDistanceBuffer;
            float retreatThreshold = config != null ? config.fleeHealthPercent : retreatHealthPercent;
            float thinkInterval = config != null ? config.thinkInterval : 0.2f;
            if (retreatWhenLowHealth && owner.GetHealth01() <= retreatThreshold)
                return TickWoundedRetreat(owner, ai, currentTarget, state, retreatSafeDistance);

            ITargetable target = AITargetScanner.FindBestTarget(owner, ai, targetMask, currentTarget, detectionRadius, state);

            if (target == null)
            {
                state.ClearAggro();
                TickPatrol(owner, ai, state);
                return null;
            }

            if (target != currentTarget)
            {
                state.GrowlPlayedForCurrentTarget = false;
                state.ChaseTimer = 0f;
            }

            state.HasAggro = true;
            AIAggroHelper.TrackVisibleTarget(owner, ai, target, state);

            Vector3 toTarget = FlatOffset(owner.transform.position, target.TargetTransform.position);
            float distance = toTarget.magnitude;

            if (AIAggroHelper.ShouldAbandonChase(owner, ai, target, state))
            {
                state.ClearAggro();
                TickPatrol(owner, ai, state);
                return null;
            }

            if (maxChaseTime > 0f
                && state.ChaseTimer >= maxChaseTime
                && distance > attackDistance * 1.2f
                && !owner.CanSee(target))
            {
                state.ClearAggro();
                TickPatrol(owner, ai, state);
                return null;
            }

            if (growlBeforeChase
                && !state.GrowlPlayedForCurrentTarget
                && AIAggroHelper.ShouldGrowlBeforeChase(owner, ai, target, attackDistance, distance))
            {
                if (TryGrowl(owner))
                {
                    state.Current = AIState.Growl;
                    state.GrowlPlayedForCurrentTarget = true;
                    state.MarkTargetSeen();
                    return target;
                }

                state.GrowlPlayedForCurrentTarget = true;
            }

            if (AICombatMovement.HandleCombatRange(owner, ai, target, attackDistance, separationBuffer))
            {
                state.MarkTargetSeen();
                return target;
            }

            state.Current = AIState.Chase;
            state.ChaseTimer += thinkInterval;
            state.ChaseSprint = AICombatMovement.ShouldSprintToTarget(owner, target, sprintDistance, attackDistance)
                || AIAggroHelper.GetTargetMoveSpeed(target) >= (config != null ? config.movingTargetSenseSpeed : 1.5f);
            state.ChaseSprint = state.ChaseSprint && !owner.IsWounded();
            state.MarkTargetSeen();

            Vector3 direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector3.zero;
            owner.Sprint(state.ChaseSprint);
            owner.Move(direction);
            owner.Rotate(direction);
            return target;
        }

        private void TickPatrol(Creature owner, AIComponent ai, AIBehaviorState state)
        {
            AIConfig config = ai.Config;
            float runChance = config != null ? config.patrolRunChance : patrolRunChance;

            if (state.WaitTimer > 0f)
            {
                state.WaitTimer -= config != null ? config.thinkInterval : 0.2f;
                state.Current = AIState.Idle;
                owner.Sprint(false);
                owner.Stop();
                return;
            }

            IZonePatrolProvider zonePatrol = ai.ZonePatrol;
            AIPatrolRoute route = ai.PatrolRoute;

            Vector3 destination;
            float reachDistance;

            if (zonePatrol != null && zonePatrol.HasPatrolPoints)
            {
                destination = zonePatrol.GetPatrolPoint(state.PatrolIndex);
                reachDistance = zonePatrol.PointReachRadius;
            }
            else if (route != null && route.HasRoute)
            {
                destination = route.GetPosition(state.PatrolIndex);
                reachDistance = route.PointReachRadius;
            }
            else
            {
                destination = GetWanderPoint(state, owner.transform.position);
                reachDistance = patrolPointReachDistance;
            }

            Vector3 toPoint = FlatOffset(owner.transform.position, destination);
            float distance = toPoint.magnitude;

            if (distance <= reachDistance)
            {
                state.WaitTimer = patrolWaitTime;
                AIPatrolPace.ResetLeg(state);

                if (zonePatrol != null && zonePatrol.HasPatrolPoints)
                    state.PatrolIndex = zonePatrol.GetRandomPatrolIndex(state.PatrolIndex);
                else if (route != null && route.HasRoute)
                    state.PatrolIndex = route.GetNextIndex(state.PatrolIndex);
                else
                    state.PatrolIndex++;

                owner.Sprint(false);
                owner.Stop();
                return;
            }

            AIPatrolPace.EnsurePaceRolled(state, config, runChance);

            state.Current = AIState.Patrol;
            Vector3 direction = toPoint.normalized;
            owner.Sprint(state.PatrolUseRun);
            owner.Move(direction);
            owner.Rotate(direction);
        }

        private ITargetable TickWoundedRetreat(Creature owner, AIComponent ai, ITargetable currentTarget, AIBehaviorState state, float safeDistance)
        {
            float detectionRadius = owner.Profile != null ? owner.Profile.detectionRadius : 18f;
            ITargetable threat = AITargetScanner.FindBestTarget(
                owner,
                ai,
                targetMask,
                currentTarget,
                detectionRadius,
                state);

            if (threat == null)
            {
                state.Current = AIState.Patrol;
                owner.Sprint(false);
                owner.Stop();
                return null;
            }

            Vector3 away = FlatOffset(threat.TargetTransform.position, owner.transform.position);
            float distance = away.magnitude;

            float attackDistance = ResolveMeleeRange(owner, ai.Config);
            float separationBuffer = ai.Config != null ? ai.Config.combatStandOffBuffer : stopDistanceBuffer;
            if (AICombatMovement.HandleCombatRange(owner, ai, threat, attackDistance, separationBuffer))
                return threat;

            if (distance >= safeDistance)
            {
                state.Current = AIState.Idle;
                owner.Sprint(false);
                owner.Stop();
                return threat;
            }

            state.Current = AIState.Flee;
            Vector3 direction = away.sqrMagnitude > 0.001f ? away.normalized : -owner.transform.forward;
            owner.Sprint(false);
            owner.Move(direction);
            owner.Rotate(direction);
            return threat;
        }

        private static float ResolveMeleeRange(Creature owner, AIConfig config)
        {
            if (owner != null && owner.Combat != null)
            {
                float meleeRange = owner.Combat.GetMeleeRange();
                if (meleeRange > 0f)
                    return meleeRange;
            }

            return config != null ? config.attackDistance : 2.5f;
        }

        private Vector3 GetWanderPoint(AIBehaviorState state, Vector3 currentPosition)
        {
            if (state.HomePosition == Vector3.zero)
                state.HomePosition = currentPosition;

            float angle = (state.PatrolIndex * 137.5f + state.HomeYaw) * Mathf.Deg2Rad;
            float radius = wanderRadius * (0.35f + (state.PatrolIndex % 3) * 0.2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            return state.HomePosition + offset;
        }

        private static bool TryGrowl(Creature owner) => owner.TryGrowl();

        private static void Face(Creature owner, Vector3 flatDirection)
        {
            if (flatDirection.sqrMagnitude > 0.001f)
                owner.Rotate(flatDirection.normalized);
        }

        private static Vector3 FlatOffset(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset;
        }
    }
}

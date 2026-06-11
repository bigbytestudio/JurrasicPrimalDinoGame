using UnityEngine;
using DinoGame.Data;
using DinoGame.Interfaces;
using DinoGame.Components;

namespace DinoGame.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(MovementComponent))]
    [RequireComponent(typeof(CombatComponent))]
    [RequireComponent(typeof(AIComponent))]
    [RequireComponent(typeof(StatusComponent))]
    [RequireComponent(typeof(AnimationComponent))]
    public abstract class Creature : MonoBehaviour, IMovable, IAttackable, ITargetable, IDamageable, IHealable
    {
        [SerializeField] private CreatureProfile profile;
        [SerializeField] private TeamType teamOverride = TeamType.Neutral;

        public CreatureProfile Profile => profile;
        public Transform TargetTransform => transform;

        public bool IsAlive => health != null && health.IsAlive;

        private TeamType? runtimeTeamOverride;

        public int TeamId => (int)(runtimeTeamOverride ?? (profile != null ? profile.defaultTeam : teamOverride));

        public void SetTeam(TeamType team) => runtimeTeamOverride = team;

        public HealthComponent Health => health;
        public MovementComponent Movement => movement;
        public CombatComponent Combat => combat;
        public AIComponent AI => ai;
        public StatusComponent Status => status;
        public AnimationComponent Animation => animationBridge;

        private HealthComponent health;
        private MovementComponent movement;
        private CombatComponent combat;
        private AIComponent ai;
        private StatusComponent status;
        private AnimationComponent animationBridge;

        protected virtual void Awake()
        {
            CacheComponents();
            InjectProfile(profile);
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;

            // Movement tick should run even if not moving,
            // because ground check and gravity need to update.
            movement.Tick(deltaTime);

            if (!IsAlive)
            {
                animationBridge.Tick(deltaTime);
                return;
            }

            status.Tick(deltaTime);
            ai.EnforceMovementLock();
            ai.Tick(deltaTime);
            animationBridge.Tick(deltaTime);
        }

        protected virtual void LateUpdate()
        {
            if (animationBridge != null)
                animationBridge.LateTick(Time.deltaTime);
        }

        public void InjectProfile(CreatureProfile newProfile)
        {
            profile = newProfile;

            CacheComponents();

            status.Initialize(this);
            health.Initialize(this, profile);
            movement.Initialize(this, profile, status);
            combat.Initialize(this, profile);
            ai.Initialize(this, profile);
            animationBridge.Initialize(this);
        }

        private void CacheComponents()
        {
            health ??= GetComponent<HealthComponent>();
            movement ??= GetComponent<MovementComponent>();
            combat ??= GetComponent<CombatComponent>();
            ai ??= GetComponent<AIComponent>();
            status ??= GetComponent<StatusComponent>();
            animationBridge ??= GetComponent<AnimationComponent>();
        }

        public void Move(Vector3 direction)
        {
            if (movement == null)
                return;

            movement.Move(direction);
        }

        public void Stop()
        {
            if (movement == null)
                return;

            movement.Stop();
        }

        public void Rotate(Vector3 direction)
        {
            if (movement == null)
                return;

            movement.Rotate(direction);
        }

        public void FaceTarget(ITargetable target)
        {
            if (target?.TargetTransform == null)
                return;

            Vector3 toTarget = target.TargetTransform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                Rotate(toTarget.normalized);
        }

        public void Sprint(bool enabled)
        {
            if (movement == null)
                return;

            movement.Sprint(enabled);
        }

        public bool CanAttack(ITargetable target)
        {
            return combat != null && combat.CanAttack(target);
        }

        public void Attack(ITargetable target)
        {
            if (combat == null)
                return;

            combat.Attack(target);
        }

        /// <summary>
        /// Faces target and plays attack animation. Damage is applied on the melee hit window.
        /// </summary>
        public bool TryCombatAttack(ITargetable target)
        {
            if (target == null || combat == null || !combat.CanAttack(target))
                return false;

            if (animationBridge != null && animationBridge.IsAttacking)
                return false;

            combat.SetPendingMeleeTarget(target);
            FaceTarget(target);

            if (animationBridge != null)
            {
                bool aiControlled = ai != null && ai.enabled;
                animationBridge.PlayAttack(aiControlled ? 0 : -1);
            }

            return true;
        }

        public void ProcessMeleeHit()
        {
            combat?.ApplyMeleeHit();
        }

        public bool TryGrowl()
        {
            if (animationBridge == null || animationBridge.IsGrowling || animationBridge.IsAttacking)
                return false;

            Stop();
            Sprint(false);
            animationBridge.PlayGrowl();
            return true;
        }

        public bool TryJump()
        {
            if (animationBridge == null || !animationBridge.CanJump())
                return false;

            animationBridge.PlayJump();
            return true;
        }

        public void UseAbility(string abilityId, ITargetable target)
        {
            if (combat == null)
                return;

            combat.UseAbility(abilityId, target);
        }

        public bool CanSee(ITargetable target)
        {
            return ai != null && ai.CanSee(target);
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (health == null)
                return;

            health.TakeDamage(amount, source);

            if (!health.IsAlive)
            {
                if (ai != null)
                    ai.enabled = false;

                movement.EndCombatPositionLock();
                movement.HaltForCombat();
                animationBridge.PlayDeath();
            }
            else
            {
                animationBridge.PlayHit();
            }
        }

        public void Heal(float amount)
        {
            if (health == null)
                return;

            health.Heal(amount);
        }

        public float GetHealth01()
        {
            return health != null ? health.GetHealth01() : 0f;
        }

        /// <summary>
        /// AI creatures move slower as health drops so wounded predators can be caught.
        /// </summary>
        public float GetWoundedSpeedMultiplier()
        {
            if (ai == null || !ai.enabled)
                return 1f;

            float health01 = GetHealth01();
            float woundedThreshold = ai.Config != null ? ai.Config.woundedHealthPercent : 0.4f;
            float minSpeed = ai.Config != null ? ai.Config.woundedMinSpeedMultiplier : 0.32f;

            if (health01 >= woundedThreshold)
                return 1f;

            if (woundedThreshold <= 0f)
                return minSpeed;

            return Mathf.Lerp(minSpeed, 1f, health01 / woundedThreshold);
        }

        public bool IsWounded()
        {
            if (ai == null || !ai.enabled)
                return false;

            float woundedThreshold = ai.Config != null ? ai.Config.woundedHealthPercent : 0.4f;
            return GetHealth01() <= woundedThreshold;
        }

        public virtual void Dispose()
        {
            if (ai != null)
                ai.Dispose();

            if (combat != null)
                combat.Dispose();

            if (status != null)
                status.Dispose();
        }
    }
}
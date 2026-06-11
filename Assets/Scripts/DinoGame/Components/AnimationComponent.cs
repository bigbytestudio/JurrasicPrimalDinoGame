using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    public sealed class AnimationComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Movement Animation")]
        [SerializeField] private float walkSpeedReference = 2.5f;
        [SerializeField] private float runSpeedReference = 6f;
        [SerializeField] private float strafeAnimMoveSpeed = 3f;
        [SerializeField] private float moveDampTime = 0.12f;
        [SerializeField] private float turnDampTime = 0.28f;
        [SerializeField] private float strafeAnimDampTime = 0.08f;
        [SerializeField] private float turnAnimReferenceAngle = 90f;

        [Header("Sleep")]
        [SerializeField] private float sleepWakeMoveSpeedSnap = 0.2f;
        [SerializeField, Range(0.5f, 1f)] private float sleepWakeUpExitNormalizedTime = 0.74f;

        [Header("Idle Random")]
        [SerializeField] private bool randomizeIdle = true;
        [SerializeField] private int idleCount = 5;
        [SerializeField] private float minIdleChangeTime = 4f;
        [SerializeField] private float maxIdleChangeTime = 8f;

        [Header("Combat Animation")]
        [SerializeField, Range(0.1f, 0.95f)] private float attackDamageNormalizedTime = 0.42f;
        [SerializeField] private float runAttackSpeedThreshold = 6f;
        [SerializeField] private int idleAttackCount = 3;
        [SerializeField] private int runAttackCount = 2;
        [SerializeField] private float growlWalkMinSpeed = 0.15f;
        [SerializeField] private float growlRunSpeedThreshold = 10f;

        [Header("Crouch")]
        [SerializeField] private float crouchWalkMinSpeed = 0.15f;
        [SerializeField] private float crouchWalkSpeedReference = 2.5f;
        [SerializeField] private float crouchTypeDampTime = 0.12f;

        [Header("Jump")]
        [SerializeField] private float jumpRunSpeedThreshold = 6f;
        [SerializeField] private float jumpTakeoffNormalizedTime = 0.4f;
        [SerializeField] private float runJumpTakeoffTime = 0.22f;
        [SerializeField] private float jumpTypeDampTime = 0.08f;
        [SerializeField] private float jumpLandDampTime = 0.04f;
        [SerializeField] private float jumpLandDuration = 0.22f;
        [SerializeField] private float runJumpLandDuration = 0.14f;
        [SerializeField] private float minJumpAirHoldTime = 0.1f;
        [SerializeField] private float runJumpMinAirHoldTime = 0.14f;
        [SerializeField] private float jumpExitFadeTime = 0.08f;
        [SerializeField] private float runJumpExitFadeTime = 0.05f;
        [SerializeField] private float jumpMoveSpeedSnapDuration = 0.2f;

        [Header("Injured Die")]
        [SerializeField] private float lowHealthDieThreshold = 0.35f;
        [SerializeField] private float dieRecoverAnimSpeed = 0.5f;
        [SerializeField, Min(0.5f)] private float deathAnimationMinDuration = 2f;
        [SerializeField, Min(0.5f)] private float deathAnimationMaxWait = 8f;

        private const float GrowlWalkType = 0f;
        private const float GrowlCrouchType = 0.5f;
        private const float GrowlRunType = 1f;

        private const float CrouchIdleType = 0f;
        private const float CrouchWalkType = 1f;

        private const int DieForwardType = 0;
        private const int DieBackwardType = 1;

        private const float JumpStartType = 0f;
        private const float JumpAirType = 0.5f;
        private const float JumpLandType = 1f;

        private const int JumpIdleVariant = 0;
        private const int JumpRunVariant = 1;

        private Creature owner;
        private MovementComponent movement;
        private CombatComponent combat;

        private float idleTimer;

        private bool sleepStarted;
        private bool wakeUpFromMovementRequested;
        private bool wakeUpMoveSpeedPrimed;
        private bool eatStarted;
        private bool attackStarted;
        private bool attackDamageApplied;
        private bool attackRootMotionStored;
        private bool attackRootMotionWasEnabled;
        private bool growlStarted;
        private bool jumpStarted;
        private bool jumpAnimEntered;
        private bool jumpImpulseApplied;
        private bool jumpWasAirborne;
        private bool isRunJump;
        private float jumpLandTimer;
        private float jumpTakeoffTimer;
        private float jumpAirTimer;
        private float jumpEnterTimeout;
        private float jumpMoveSpeedSnapTimer;
        private bool attackStopsMovement;
        private bool isCrouching;
        private bool isInjuredDown;
        private bool dieRecoverStarted;
        private bool isPermanentlyDead;
        private bool deathAnimationStarted;
        private bool deathAnimationComplete;
        private float deathStateTimer;
        private int activeEatStyle;
        private int eatThresholdStep;
        private bool eatStepCycleHandled;

        private static readonly float[] EatBlendThresholds = { 0f, 1f };

        private static readonly int EatStyleHash = Animator.StringToHash("EatStyle");
        private static readonly int EatStyle1StateHash = Animator.StringToHash("EatStyle_1");
        private static readonly int EatStyle2StateHash = Animator.StringToHash("EatStyle_2");

        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int TurnSpeedHash = Animator.StringToHash("TurnSpeed");
        private static readonly int IdleTypeHash = Animator.StringToHash("IdleType");

        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int IsSleepingHash = Animator.StringToHash("IsSleeping");

        private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
        private static readonly int EatTypeHash = Animator.StringToHash("EatType");
        private static readonly int GrowlTypeHash = Animator.StringToHash("GrowlType");

        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private static readonly int GrowlTriggerHash = Animator.StringToHash("GrowlTrigger");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");
        private static readonly int DieTriggerHash = Animator.StringToHash("DieTrigger");
        private static readonly int DieTypeHash = Animator.StringToHash("DieType");
        private static readonly int IsInjuredHash = Animator.StringToHash("IsInjured");
        private static readonly int CrouchTypeHash = Animator.StringToHash("CrouchType");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int JumpTypeHash = Animator.StringToHash("JumpType");
        private static readonly int JumpVariantHash = Animator.StringToHash("JumpVariant");
        private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int SleepTriggerHash = Animator.StringToHash("SleepTrigger");
        private static readonly int WakeUpTriggerHash = Animator.StringToHash("WakeUpTrigger");
        private static readonly int EatTriggerHash = Animator.StringToHash("EatTrigger");
        private static readonly int IsEattingHash = Animator.StringToHash("IsEatting");

        private static readonly int[] IdleAttackStateHashes =
        {
            Animator.StringToHash("Dilo|IdleAtk1"),
            Animator.StringToHash("Dilo|IdleAtk2"),
            Animator.StringToHash("Dilo|IdleAtk3"),
        };

        private static readonly int[] RunAttackStateHashes =
        {
            Animator.StringToHash("Dilo|RunAtk1"),
            Animator.StringToHash("Dilo|RunAtk2"),
        };

        private static readonly int GrowlStateHash = Animator.StringToHash("Growl");
        private static readonly int CrouchStateHash = Animator.StringToHash("Crouch");
        private static readonly int LocomotionStateHash = Animator.StringToHash("Locomotion");
        private static readonly int DieForwardStateHash = Animator.StringToHash("Dilo|Die");
        private static readonly int DieBackwardStateHash = Animator.StringToHash("Dilo|Die-");
        private static readonly int IdleJumpStateHash = Animator.StringToHash("IdleJump");
        private static readonly int RunJumpStateHash = Animator.StringToHash("RunJump");
        private static readonly int WakeUpFromSleepStateHash = Animator.StringToHash("WakeUpFromSleep");

        public void Initialize(Creature creature)
        {
            owner = creature;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            movement ??= GetComponent<MovementComponent>();
            combat ??= GetComponent<CombatComponent>();

            ResetIdleTimer();

            isPermanentlyDead = false;
            deathAnimationStarted = false;
            deathAnimationComplete = false;
            deathStateTimer = 0f;

            if (animator != null && owner != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsDeadHash, false);
                animator.SetBool(IsSleepingHash, false);
                animator.SetBool(IsEattingHash, false);
                animator.SetBool(IsInjuredHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsJumpingHash, false);
                animator.SetFloat(MoveSpeedHash, 0f);
            }
        }

        public void Tick(float deltaTime)
        {
            if (animator == null || owner == null)
                return;

            UpdateLifeState();

            if (isPermanentlyDead)
            {
                UpdateDeathState();
                return;
            }

            UpdateMovement(deltaTime);
            UpdateIdleRandom(deltaTime);
            UpdateSleepState();
            UpdateWakeUpState();
            UpdateEatSequence();
            UpdateAttackState();
            UpdateGrowlState();
            UpdateCrouchState(deltaTime);
            UpdateJumpState(deltaTime);
            UpdateInjuredState();
        }

        public void LateTick(float deltaTime)
        {
            UpdateCombatFacing();

            if (!attackStarted || owner == null || owner.AI == null || !owner.AI.enabled)
                return;

            movement?.MaintainCombatPositionLock();
        }

        private void UpdateCombatFacing()
        {
            if (!attackStarted || owner == null)
                return;

            ITargetable focus = combat != null ? combat.ResolveAttackFocus() : null;
            if (focus == null)
                return;

            owner.FaceTarget(focus);
        }

        private void UpdateLifeState()
        {
            animator.SetBool(IsDeadHash, !owner.IsAlive);
        }

        private void UpdateMovement(float deltaTime)
        {
            if (movement == null)
                return;

            if (IsWakingUp)
            {
                animator.SetFloat(MoveSpeedHash, 0f);
                return;
            }

            float rawSpeed = movement.Velocity.magnitude;
            float animSpeed = isCrouching
                ? Mathf.Min(rawSpeed, crouchWalkSpeedReference)
                : rawSpeed;

            if (jumpMoveSpeedSnapTimer > 0f)
            {
                jumpMoveSpeedSnapTimer -= deltaTime;
                animator.SetFloat(MoveSpeedHash, animSpeed);
            }
            else
            {
                animator.SetFloat(MoveSpeedHash, animSpeed, moveDampTime, deltaTime);
            }
            float turnAnim = movement.IsArcLocomotion
                ? Mathf.Clamp(movement.LeanAngle / turnAnimReferenceAngle, -1f, 1f)
                : CalculateTurnSpeed();
            animator.SetFloat(TurnSpeedHash, turnAnim, turnDampTime, deltaTime);
            animator.SetBool(IsGroundedHash, movement.IsGrounded);
        }

        private float NormalizeMoveSpeed(float speed)
        {
            if (speed <= 0.05f)
                return 0f;

            if (speed <= walkSpeedReference)
                return Mathf.InverseLerp(0f, walkSpeedReference, speed) * 0.5f;

            return Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(walkSpeedReference, runSpeedReference, speed));
        }

        private float CalculateTurnSpeed()
        {
            if (movement == null || turnAnimReferenceAngle <= 0.01f)
                return 0f;

            return Mathf.Clamp(movement.FacingTurnAngle / turnAnimReferenceAngle, -1f, 1f);
        }

        private void UpdateIdleRandom(float deltaTime)
        {
            if (!randomizeIdle || idleCount <= 1)
                return;

            float moveSpeed = animator.GetFloat(MoveSpeedHash);

            if (moveSpeed > 0.05f)
            {
                ResetIdleTimer();
                return;
            }

            if (animator.GetBool(IsDeadHash) || animator.GetBool(IsSleepingHash) || sleepStarted || IsWakingUp || eatStarted
                || attackStarted || growlStarted || jumpStarted || isInjuredDown)
            {
                ResetIdleTimer();
                return;
            }

            idleTimer -= deltaTime;

            if (idleTimer <= 0f)
            {
                int idleIndex = Random.Range(0, idleCount);
                animator.SetFloat(IdleTypeHash, idleIndex);
                ResetIdleTimer();
            }
        }

        private void ResetIdleTimer()
        {
            idleTimer = Random.Range(minIdleChangeTime, maxIdleChangeTime);
        }

        private void UpdateSleepState()
        {
            if (!sleepStarted || animator.GetBool(IsSleepingHash))
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Sleep"))
                animator.SetBool(IsSleepingHash, true);
        }

        private void UpdateWakeUpState()
        {
            if (!wakeUpFromMovementRequested || animator == null)
                return;

            if (animator.IsInTransition(0))
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (IsWakeUpState(state))
            {
                if (!wakeUpMoveSpeedPrimed && state.normalizedTime >= sleepWakeUpExitNormalizedTime)
                {
                    animator.SetFloat(MoveSpeedHash, sleepWakeMoveSpeedSnap);
                    wakeUpMoveSpeedPrimed = true;
                }

                if (state.normalizedTime < 1f)
                    return;
            }

            wakeUpFromMovementRequested = false;
            wakeUpMoveSpeedPrimed = false;
        }

        private static bool IsWakeUpState(AnimatorStateInfo state)
        {
            return state.shortNameHash == WakeUpFromSleepStateHash || state.IsName("WakeUpFromSleep");
        }

        private void UpdateEatSequence()
        {
            if (!eatStarted || !IsInEatStyleState())
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.normalizedTime >= 1f)
            {
                if (eatStepCycleHandled)
                    return;

                eatStepCycleHandled = true;

                if (eatThresholdStep < EatBlendThresholds.Length - 1)
                {
                    eatThresholdStep++;
                    eatStepCycleHandled = false;
                    animator.SetFloat(EatTypeHash, EatBlendThresholds[eatThresholdStep]);
                    animator.Play(state.shortNameHash, 0, 0f);
                }
                else
                {
                    StopEat();
                }
            }
            else if (state.normalizedTime < 0.15f)
            {
                eatStepCycleHandled = false;
            }
        }

        private bool IsInEatStyleState()
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.shortNameHash == EatStyle1StateHash || state.shortNameHash == EatStyle2StateHash;
        }

        public void PlayEat()
        {
            if (animator == null || IsEating)
                return;

            eatStarted = true;
            eatThresholdStep = 0;
            eatStepCycleHandled = false;

            activeEatStyle = Random.Range(0, 2);

            animator.SetInteger(EatStyleHash, activeEatStyle);
            animator.SetFloat(EatTypeHash, EatBlendThresholds[0]);
            animator.SetBool(IsEattingHash, true);
            animator.ResetTrigger(EatTriggerHash);
            animator.SetTrigger(EatTriggerHash);
        }

        public void StopEat()
        {
            if (animator == null || !eatStarted)
                return;

            eatStarted = false;
            eatThresholdStep = 0;
            eatStepCycleHandled = false;
            animator.ResetTrigger(EatTriggerHash);
            animator.SetBool(IsEattingHash, false);
        }

        public bool IsEating
        {
            get
            {
                if (eatStarted)
                    return true;

                if (animator == null)
                    return false;

                return animator.GetBool(IsEattingHash) || IsInEatStyleState();
            }
        }

        public void PlayAttack(int attackType = -1)
        {
            if (animator == null || attackStarted || IsEating || growlStarted || jumpStarted || sleepStarted
                || isInjuredDown || isCrouching)
                return;

            if (owner != null && !owner.IsAlive)
                return;

            bool isAiControlled = owner != null && owner.AI != null && owner.AI.enabled;
            if (isAiControlled)
                attackType = 0;

            bool isRunningAttack = ResolveRunningAttack(attackType);
            int attackIndex = ResolveAttackIndex(attackType, isRunningAttack);
            int stateHash = GetAttackStateHash(isRunningAttack, attackIndex);
            int attackTypeParam = isRunningAttack ? attackIndex + 3 : attackIndex;

            attackStarted = true;
            attackDamageApplied = false;
            attackStopsMovement = !isRunningAttack;

            if (isAiControlled && animator != null)
            {
                attackRootMotionStored = true;
                attackRootMotionWasEnabled = animator.applyRootMotion;
                animator.applyRootMotion = false;
            }

            if (movement != null)
            {
                animator.SetFloat(MoveSpeedHash, isAiControlled ? 0f : movement.CurrentMoveSpeed);
                if (isAiControlled)
                    movement.BeginCombatPositionLock();
            }

            animator.SetInteger(AttackTypeHash, attackTypeParam);
            animator.ResetTrigger(AttackTriggerHash);
            animator.CrossFadeInFixedTime(stateHash, 0.1f, 0, 0f);
        }

        private bool ResolveRunningAttack(int attackType)
        {
            if (attackType >= 3)
                return true;

            if (attackType >= 0 && attackType < idleAttackCount)
                return false;

            return IsRunningForAttack();
        }

        private int ResolveAttackIndex(int attackType, bool isRunningAttack)
        {
            int count = isRunningAttack ? runAttackCount : idleAttackCount;
            count = Mathf.Max(1, count);

            if (attackType >= 0)
            {
                if (isRunningAttack)
                    return Mathf.Clamp(attackType - 3, 0, count - 1);

                return Mathf.Clamp(attackType, 0, count - 1);
            }

            return Random.Range(0, count);
        }

        private static int GetAttackStateHash(bool isRunningAttack, int attackIndex)
        {
            if (isRunningAttack)
                return RunAttackStateHashes[Mathf.Clamp(attackIndex, 0, RunAttackStateHashes.Length - 1)];

            return IdleAttackStateHashes[Mathf.Clamp(attackIndex, 0, IdleAttackStateHashes.Length - 1)];
        }

        public void PlayHit()
        {
            if (animator == null)
                return;

            animator.ResetTrigger(HitTriggerHash);
            animator.SetTrigger(HitTriggerHash);
        }

        public void PlayDeath()
        {
            if (animator == null || isPermanentlyDead)
                return;

            isPermanentlyDead = true;
            deathAnimationStarted = true;
            deathAnimationComplete = false;
            deathStateTimer = 0f;
            isInjuredDown = false;
            dieRecoverStarted = false;
            isCrouching = false;
            sleepStarted = false;
            eatStarted = false;
            growlStarted = false;
            jumpStarted = false;
            EndAttack();

            movement?.EndCombatPositionLock();
            movement?.HaltForCombat();

            animator.speed = 1f;
            animator.SetFloat(MoveSpeedHash, 0f);
            animator.SetFloat(TurnSpeedHash, 0f);
            animator.SetBool(IsSleepingHash, false);
            animator.SetBool(IsEattingHash, false);
            animator.SetBool(IsJumpingHash, false);
            animator.SetBool(IsInjuredHash, false);
            animator.SetBool(IsCrouchingHash, false);
            animator.SetBool(IsDeadHash, true);
            animator.SetInteger(DieTypeHash, DieForwardType);
            animator.ResetTrigger(DieTriggerHash);
            animator.SetTrigger(DieTriggerHash);
            animator.CrossFadeInFixedTime(DieForwardStateHash, 0.12f, 0, 0f);
        }

        public bool IsPlayingDeath => deathAnimationStarted && !deathAnimationComplete;

        public bool IsDeathAnimationComplete => deathAnimationComplete;

        public float DeathAnimationMaxWait => deathAnimationMaxWait;

        public bool CanPlayInjuredDown()
        {
            if (animator == null || owner == null || !owner.IsAlive || isPermanentlyDead)
                return false;

            if (isInjuredDown || sleepStarted || IsEating || attackStarted || growlStarted)
                return false;

            return owner.GetHealth01() <= lowHealthDieThreshold;
        }

        public void PlayInjuredDown()
        {
            if (animator == null || !CanPlayInjuredDown())
                return;

            isInjuredDown = true;
            dieRecoverStarted = false;
            isCrouching = false;

            animator.SetBool(IsInjuredHash, true);
            animator.SetBool(IsCrouchingHash, false);
            animator.SetInteger(DieTypeHash, DieForwardType);
            animator.ResetTrigger(DieTriggerHash);
            animator.SetTrigger(DieTriggerHash);
        }

        public void PlayInjuredUp()
        {
            if (animator == null || !isInjuredDown || dieRecoverStarted || isPermanentlyDead)
                return;

            dieRecoverStarted = true;
            animator.speed = dieRecoverAnimSpeed;
            animator.SetInteger(DieTypeHash, DieBackwardType);
            animator.CrossFadeInFixedTime(DieBackwardStateHash, 0.15f, 0, 0f);
        }

        public void SetCrouching(bool crouching)
        {
            if (isInjuredDown || isPermanentlyDead || sleepStarted || IsEating || jumpStarted)
            {
                isCrouching = false;
                animator?.SetBool(IsCrouchingHash, false);
                return;
            }

            isCrouching = crouching;
            animator?.SetBool(IsCrouchingHash, crouching);
        }

        public bool CanJump()
        {
            if (animator == null || owner == null || !owner.IsAlive || isPermanentlyDead)
                return false;

            if (jumpStarted || attackStarted || growlStarted || sleepStarted || IsEating
                || isInjuredDown || isCrouching)
                return false;

            return movement == null || movement.IsGrounded && !movement.IsJumping;
        }

        public void PlayJump()
        {
            if (animator == null || !CanJump())
                return;

            isRunJump = ResolveRunJump();
            jumpStarted = true;
            jumpAnimEntered = false;
            jumpImpulseApplied = false;
            jumpWasAirborne = false;
            jumpLandTimer = 0f;
            jumpTakeoffTimer = 0f;
            jumpAirTimer = 0f;
            jumpEnterTimeout = 0.5f;
            isCrouching = false;

            animator.SetBool(IsCrouchingHash, false);

            if (movement != null)
            {
                animator.SetBool(IsGroundedHash, movement.IsGrounded);
                animator.SetFloat(MoveSpeedHash, movement.CurrentMoveSpeed);
            }

            animator.SetInteger(JumpVariantHash, isRunJump ? JumpRunVariant : JumpIdleVariant);
            animator.SetFloat(JumpTypeHash, JumpStartType);
            animator.SetBool(IsJumpingHash, true);
            animator.ResetTrigger(JumpTriggerHash);
            animator.SetTrigger(JumpTriggerHash);

            movement?.BeginJump(isRunJump);
        }

        public void PlaySleep()
        {
            if (animator == null)
                return;

            if (sleepStarted)
                return;

            sleepStarted = true;

            animator.ResetTrigger(WakeUpTriggerHash);
            animator.ResetTrigger(SleepTriggerHash);
            animator.SetBool(IsSleepingHash, false);
            animator.SetTrigger(SleepTriggerHash);
        }

        public void PlayWakeUp()
        {
            if (animator == null || (!sleepStarted && !IsSleeping))
                return;

            sleepStarted = false;

            animator.ResetTrigger(SleepTriggerHash);
            animator.ResetTrigger(WakeUpTriggerHash);
            animator.SetBool(IsSleepingHash, false);
            animator.SetTrigger(WakeUpTriggerHash);
        }

        public bool IsSleeping => animator != null && animator.GetBool(IsSleepingHash);

        public bool IsWakingUp => wakeUpFromMovementRequested;

        public bool IsInSleepFlow => sleepStarted || IsSleeping;

        /// <summary>
        /// Plays WakeUpFromSleep first; movement is allowed after the wake animation finishes.
        /// </summary>
        public void RequestWakeUpFromMovement()
        {
            if (animator == null || IsWakingUp || !IsInSleepFlow)
                return;

            wakeUpFromMovementRequested = true;
            wakeUpMoveSpeedPrimed = false;
            animator.SetFloat(MoveSpeedHash, 0f);
            PlayWakeUp();
        }

        public void PlayGrowl(float growlType = -1f)
        {
            if (animator == null || growlStarted || attackStarted || IsEating || jumpStarted || sleepStarted
                || isInjuredDown || isCrouching)
                return;

            if (owner != null && !owner.IsAlive)
                return;

            if (growlType < 0f)
                growlType = ResolveGrowlType();

            growlStarted = true;

            if (movement != null)
                animator.SetFloat(MoveSpeedHash, movement.CurrentMoveSpeed);

            animator.SetFloat(GrowlTypeHash, growlType);
            animator.ResetTrigger(GrowlTriggerHash);
            animator.CrossFadeInFixedTime(GrowlStateHash, 0.1f, 0, 0f);
        }

        public bool IsAttacking => attackStarted;

        public bool AttackStopsMovement => attackStopsMovement;

        public bool IsGrowling => growlStarted;

        public bool IsJumping => jumpStarted;

        public bool IsCrouching => isCrouching;

        public bool IsInjuredDown => isInjuredDown;

        private void UpdateCrouchState(float deltaTime)
        {
            if (animator == null || isInjuredDown || isPermanentlyDead)
                return;

            if (!isCrouching)
                return;

            float speed = movement != null ? movement.CurrentMoveSpeed : 0f;
            float targetCrouchType = CrouchIdleType;

            if (speed > crouchWalkMinSpeed && crouchWalkSpeedReference > crouchWalkMinSpeed)
                targetCrouchType = Mathf.Clamp01((speed - crouchWalkMinSpeed) / (crouchWalkSpeedReference - crouchWalkMinSpeed));

            animator.SetFloat(CrouchTypeHash, targetCrouchType, crouchTypeDampTime, deltaTime);
        }

        private void UpdateDeathState()
        {
            if (!deathAnimationStarted || deathAnimationComplete || animator == null)
                return;

            deathStateTimer += Time.deltaTime;

            if (!animator.IsInTransition(0))
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (IsInDieState(state) && state.normalizedTime >= 0.95f)
                {
                    deathAnimationComplete = true;
                    return;
                }
            }

            if (deathStateTimer >= deathAnimationMinDuration && !animator.IsInTransition(0))
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (IsInDieState(state))
                    deathAnimationComplete = true;
            }

            if (deathStateTimer >= deathAnimationMaxWait)
                deathAnimationComplete = true;
        }

        private static bool IsInDieState(AnimatorStateInfo state)
        {
            int shortHash = state.shortNameHash;
            return shortHash == DieForwardStateHash
                || shortHash == DieBackwardStateHash
                || state.IsName("Dilo|Die")
                || state.IsName("Dilo|Die-");
        }

        private void UpdateInjuredState()
        {
            if (!dieRecoverStarted || animator == null)
                return;

            if (animator.IsInTransition(0))
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.shortNameHash != DieBackwardStateHash)
                return;

            if (state.normalizedTime < 1f)
                return;

            animator.speed = 1f;
            isInjuredDown = false;
            dieRecoverStarted = false;
            animator.SetBool(IsInjuredHash, false);
        }

        private bool IsRunningForAttack()
        {
            if (movement == null)
                return false;

            if (movement.IsSprinting && movement.CurrentMoveSpeed > growlWalkMinSpeed)
                return true;

            return movement.CurrentMoveSpeed >= runAttackSpeedThreshold;
        }

        private float ResolveGrowlType()
        {
            if (movement == null)
                return GrowlCrouchType;

            if (IsRunningForGrowl())
                return GrowlRunType;

            if (IsWalkingForGrowl())
                return GrowlWalkType;

            return GrowlCrouchType;
        }

        private bool IsRunningForGrowl()
        {
            if (movement == null)
                return false;

            if (movement.IsSprinting && movement.CurrentMoveSpeed > growlWalkMinSpeed)
                return true;

            return movement.CurrentMoveSpeed >= growlRunSpeedThreshold;
        }

        private bool IsWalkingForGrowl()
        {
            if (movement == null)
                return false;

            return movement.CurrentMoveSpeed > growlWalkMinSpeed && !IsRunningForGrowl();
        }

        private void UpdateAttackState()
        {
            if (!attackStarted)
                return;

            if (animator.IsInTransition(0))
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!IsInAttackState(state))
            {
                EndAttack();
                return;
            }

            if (!attackDamageApplied && state.normalizedTime >= attackDamageNormalizedTime)
            {
                attackDamageApplied = true;
                owner?.ProcessMeleeHit();
            }

            if (state.normalizedTime >= 1f)
                EndAttack();
        }

        private bool IsInJumpAnimState(AnimatorStateInfo state)
        {
            return state.shortNameHash == IdleJumpStateHash
                || state.shortNameHash == RunJumpStateHash
                || state.IsName("IdleJump")
                || state.IsName("RunJump")
                || state.IsName("Jump.IdleJump")
                || state.IsName("Jump.RunJump");
        }

        private void UpdateJumpState(float deltaTime)
        {
            if (!jumpStarted || animator == null)
                return;

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
                if (IsInJumpAnimState(nextState))
                    jumpAnimEntered = true;

                return;
            }

            jumpEnterTimeout -= deltaTime;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!IsInJumpAnimState(state))
            {
                if (jumpAnimEntered)
                {
                    EndJump();
                    return;
                }

                if (jumpEnterTimeout <= 0f)
                    EndJump();

                return;
            }

            jumpAnimEntered = true;

            if (!jumpImpulseApplied)
            {
                jumpTakeoffTimer += deltaTime;

                bool takeoffComplete = isRunJump
                    ? jumpTakeoffTimer >= runJumpTakeoffTime
                    : state.normalizedTime >= jumpTakeoffNormalizedTime;

                if (takeoffComplete)
                {
                    movement?.ApplyJumpImpulse();
                    jumpImpulseApplied = true;
                }

                animator.SetFloat(JumpTypeHash, JumpStartType, jumpTypeDampTime, deltaTime);
                return;
            }

            if (movement != null && !movement.IsGrounded)
            {
                jumpWasAirborne = true;
                jumpAirTimer += deltaTime;
                jumpLandTimer = 0f;
                SetJumpTypeImmediate(JumpAirType);
                return;
            }

            if (jumpWasAirborne)
            {
                bool stillInAir = movement == null || !movement.IsGrounded;
                float minAirHold = isRunJump ? runJumpMinAirHoldTime : minJumpAirHoldTime;

                if (stillInAir || jumpAirTimer < minAirHold)
                {
                    if (stillInAir)
                        jumpAirTimer += deltaTime;

                    jumpLandTimer = 0f;
                    SetJumpTypeImmediate(JumpAirType);
                    return;
                }

                jumpLandTimer += deltaTime;
                animator.SetFloat(JumpTypeHash, JumpLandType, jumpLandDampTime, deltaTime);

                float landDuration = isRunJump ? runJumpLandDuration : jumpLandDuration;
                if (jumpLandTimer >= landDuration)
                    EndJump();

                return;
            }

            animator.SetFloat(JumpTypeHash, JumpStartType, jumpTypeDampTime, deltaTime);
        }

        private void SetJumpTypeImmediate(float jumpType)
        {
            animator.SetFloat(JumpTypeHash, jumpType);
        }

        private bool ResolveRunJump()
        {
            if (movement == null)
                return false;

            if (movement.IsSprinting && movement.CurrentMoveSpeed > growlWalkMinSpeed)
                return true;

            return movement.CurrentMoveSpeed >= jumpRunSpeedThreshold;
        }

        private void EndJump()
        {
            bool wasRunJump = isRunJump;

            jumpStarted = false;
            jumpAnimEntered = false;
            jumpImpulseApplied = false;
            jumpWasAirborne = false;
            jumpLandTimer = 0f;
            jumpTakeoffTimer = 0f;
            jumpAirTimer = 0f;
            jumpEnterTimeout = 0f;
            isRunJump = false;

            movement?.EndJump();

            if (animator != null)
            {
                if (movement != null)
                {
                    float exitMoveSpeed = movement.CurrentMoveSpeed;
                    if (wasRunJump && exitMoveSpeed < runSpeedReference)
                        exitMoveSpeed = runSpeedReference;

                    animator.SetFloat(MoveSpeedHash, exitMoveSpeed);
                    jumpMoveSpeedSnapTimer = wasRunJump
                        ? jumpMoveSpeedSnapDuration
                        : jumpMoveSpeedSnapDuration * 0.75f;
                }

                animator.SetBool(IsJumpingHash, false);
                float exitFade = wasRunJump ? runJumpExitFadeTime : jumpExitFadeTime;
                animator.CrossFadeInFixedTime(LocomotionStateHash, exitFade, 0, 0f);
            }
        }

        private void UpdateGrowlState()
        {
            if (!growlStarted)
                return;

            if (animator.IsInTransition(0))
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName("Growl"))
            {
                growlStarted = false;
                return;
            }

            if (state.normalizedTime >= 1f)
                growlStarted = false;
        }

        private void EndAttack()
        {
            if (attackRootMotionStored && animator != null)
            {
                animator.applyRootMotion = attackRootMotionWasEnabled;
                attackRootMotionStored = false;
            }

            movement?.EndCombatPositionLock();
            combat?.ClearAttackFocus();

            attackStarted = false;
            attackDamageApplied = false;
            attackStopsMovement = false;
        }

        private bool IsInAttackState(AnimatorStateInfo state)
        {
            int shortHash = state.shortNameHash;

            for (int i = 0; i < IdleAttackStateHashes.Length; i++)
            {
                if (shortHash == IdleAttackStateHashes[i])
                    return true;
            }

            for (int i = 0; i < RunAttackStateHashes.Length; i++)
            {
                if (shortHash == RunAttackStateHashes[i])
                    return true;
            }

            return false;
        }

        public void SetIdleType(int idleType)
        {
            if (animator == null)
                return;

            animator.SetFloat(IdleTypeHash, idleType);
        }

        public void SetSleeping(bool value)
        {
            if (animator == null)
                return;

            animator.SetBool(IsSleepingHash, value);
        }

        public void SetGrounded(bool value)
        {
            if (animator == null)
                return;

            animator.SetBool(IsGroundedHash, value);
        }

        public bool IsInState(string stateName, int layerIndex = 0)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.IsName(stateName);
        }
    }
}
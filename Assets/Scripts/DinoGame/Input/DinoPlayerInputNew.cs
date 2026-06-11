using UnityEngine;
using UnityEngine.InputSystem;
using DinoGame.AI;
using DinoGame.Combat;
using DinoGame.Components;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Input
{
    [DisallowMultipleComponent]
    public sealed class DinoPlayerInputNew : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Creature creature;
        [SerializeField] private MovementComponent movement;
        [SerializeField] private AnimationComponent animationComponent;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private InputActionReference sleepAction;
        [SerializeField] private InputActionReference wakeUpAction;
        [SerializeField] private InputActionReference hitAction;
        [SerializeField] private InputActionReference dieAction;
        [SerializeField] private InputActionReference eatAction;
        [SerializeField] private InputActionReference growlAction;
        [SerializeField] private InputActionReference crouchAction;
        [SerializeField] private InputActionReference jumpAction;


        private bool inputLocked;
        private bool sprintToggled;
        private int lastEatInputFrame = -1;
        private InputAction crouchActionRuntime;
        private InputAction jumpActionRuntime;


        private void Awake()
        {
            if (creature == null)
                creature = GetComponent<Creature>();

            if (movement == null)
                movement = GetComponent<MovementComponent>();

            if (animationComponent == null)
                animationComponent = GetComponent<AnimationComponent>();

            ResolveOptionalActions();
        }

        private void ResolveOptionalActions()
        {
            if (moveAction == null)
                return;

            InputActionAsset asset = moveAction.action?.actionMap?.asset;
            if (asset == null)
                return;

            if (crouchAction == null)
                crouchActionRuntime = asset.FindAction("Crouch", false);

            if (jumpAction == null)
                jumpActionRuntime = asset.FindAction("Jump", false);
        }

        private bool IsCrouchPressed()
        {
            if (crouchAction != null)
                return crouchAction.action.IsPressed();

            return crouchActionRuntime != null && crouchActionRuntime.IsPressed();
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(sprintAction);
            EnableAction(crouchAction);
            EnableAction(jumpAction);
            crouchActionRuntime?.Enable();
            jumpActionRuntime?.Enable();

            Register(sprintAction, OnSprint);
            Register(attackAction, OnAttack);
            Register(sleepAction, OnSleep);
            Register(wakeUpAction, OnWakeUp);
            Register(hitAction, OnHit);
            Register(dieAction, OnDie);
            Register(eatAction, OnEat);
            Register(growlAction, OnGrowl);
            Register(jumpAction, OnJump);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(sprintAction);
            DisableAction(crouchAction);
            DisableAction(jumpAction);
            crouchActionRuntime?.Disable();
            jumpActionRuntime?.Disable();

            Unregister(sprintAction, OnSprint);
            Unregister(attackAction, OnAttack);
            Unregister(sleepAction, OnSleep);
            Unregister(wakeUpAction, OnWakeUp);
            Unregister(hitAction, OnHit);
            Unregister(dieAction, OnDie);
            Unregister(eatAction, OnEat);
            Unregister(growlAction, OnGrowl);
            Unregister(jumpAction, OnJump);
        }

        private void Update()
        {
            if (creature != null && !creature.IsAlive)
            {
                movement?.Stop();
                return;
            }

            UpdateCrouchInput();
            HandleMovement();
        }

        private void UpdateCrouchInput()
        {
            if (animationComponent == null)
                return;

            bool wantsCrouch = IsCrouchPressed();
            animationComponent.SetCrouching(wantsCrouch);

            if (movement != null)
                movement.SetCrouching(wantsCrouch);
        }

        private void HandleMovement()
        {
            if (movement == null)
                return;

            if (animationComponent != null && animationComponent.IsInjuredDown)
            {
                movement.Stop();
                return;
            }

            Vector2 moveInput = moveAction != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

            if (inputDirection.sqrMagnitude > 1f)
                inputDirection.Normalize();

            if (animationComponent != null && animationComponent.IsWakingUp)
            {
                movement.Stop();
                return;
            }

            if (animationComponent != null && animationComponent.IsInSleepFlow)
            {
                if (inputDirection.sqrMagnitude > 0.01f)
                    animationComponent.RequestWakeUpFromMovement();

                movement.Stop();
                return;
            }

            if (animationComponent != null && !animationComponent.IsInSleepFlow && inputLocked)
                inputLocked = false;

            if (inputLocked)
                return;

            if (animationComponent != null && animationComponent.IsEating)
            {
                if (inputDirection.sqrMagnitude > 0.01f)
                    animationComponent.StopEat();
                else
                {
                    movement.Stop();
                    return;
                }
            }

            if (animationComponent != null && animationComponent.IsGrowling)
            {
                movement.Stop();
                return;
            }

            bool isSprinting = sprintToggled
                && (animationComponent == null || !animationComponent.IsCrouching);

            if (animationComponent != null && animationComponent.IsJumping)
            {
                if (inputDirection.sqrMagnitude > 0.01f)
                {
                    Vector3 worldDirection = GetCameraRelativeDirection(inputDirection);
                    movement.Sprint(isSprinting);
                    movement.Move(worldDirection);
                }

                return;
            }

            if (animationComponent != null && animationComponent.IsAttacking && animationComponent.AttackStopsMovement)
            {
                movement.Stop();
                return;
            }

            if (inputDirection.sqrMagnitude > 0.01f)
            {
                Vector3 worldDirection = GetCameraRelativeDirection(inputDirection);

                if (ShouldBlockMovementIntoMelee(creature, worldDirection))
                {
                    movement.Sprint(isSprinting);
                    movement.Stop();
                    return;
                }

                movement.Sprint(isSprinting);
                movement.Move(worldDirection);
            }
            else
            {
                movement.Sprint(isSprinting);
                movement.Stop();
            }
        }

        private bool ShouldBlockMovementIntoMelee(Creature source, Vector3 worldDirection)
        {
            if (source == null || movement == null)
                return false;

            ITargetable hostile = FindMeleeTarget(source);
            if (hostile == null)
                return false;

            const float meleeRange = 2.2f;
            const float standOffBuffer = 0.5f;
            float approachDot = Vector3.Dot(
                worldDirection.normalized,
                CombatSpacing.GetApproachDirection(source, hostile));

            if (approachDot < 0.2f)
                return false;

            return movement.IsBlockedByHostileSpacing(hostile, meleeRange, standOffBuffer);
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            if (!context.performed || inputLocked)
                return;

            sprintToggled = !sprintToggled;
        }

        private Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
        {
            if (Camera.main == null)
                return inputDirection;

            Transform cam = Camera.main.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 worldDirection = forward * inputDirection.z + right * inputDirection.x;

            if (worldDirection.sqrMagnitude > 1f)
                worldDirection.Normalize();

            return worldDirection;
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            Attack();
        }

        private void OnSleep(InputAction.CallbackContext context)
        {
            if (context.performed)
                Sleep();
        }

        private void OnWakeUp(InputAction.CallbackContext context)
        {
            if (context.performed)
                WakeUp();
        }

        private void OnHit(InputAction.CallbackContext context)
        {
            if (context.performed)
                Hit();
        }

        private void OnDie(InputAction.CallbackContext context)
        {
            if (context.performed)
                Die();
        }

        private void OnEat(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (Time.frameCount == lastEatInputFrame)
                return;

            lastEatInputFrame = Time.frameCount;
            Eat();
        }

        private void OnGrowl(InputAction.CallbackContext context)
        {
            if (context.performed)
                Growl();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                Jump();
        }

        public void Attack()
        {
            if (inputLocked || animationComponent == null)
                return;

            if (animationComponent.IsAttacking || animationComponent.IsEating || animationComponent.IsGrowling)
                return;

            animationComponent.StopEat();

            ITargetable target = creature != null ? FindMeleeTarget(creature) : null;

            if (creature != null && creature.Combat != null)
                creature.Combat.SetPendingMeleeTarget(target);

            if (target != null)
                creature.FaceTarget(target);

            animationComponent.PlayAttack();
        }

        private static ITargetable FindMeleeTarget(Creature source)
        {
            if (source == null)
                return null;

            Collider[] hits = Physics.OverlapSphere(source.transform.position, 3f, ~0, CombatPhysics.TargetQuery);
            ITargetable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TargetableResolver.TryResolve(hits[i], out ITargetable candidate))
                    continue;

                if (!candidate.IsAlive || candidate.TargetTransform == source.transform)
                    continue;

                if (candidate.TeamId == source.TeamId)
                    continue;

                float sqr = (candidate.TargetTransform.position - source.transform.position).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                best = candidate;
                bestSqr = sqr;
            }

            return best;
        }

        public void Sleep()
        {
            if (inputLocked || animationComponent == null)
                return;

            animationComponent.StopEat();
            inputLocked = true;

            if (movement != null)
            {
                sprintToggled = false;
                movement.Sprint(false);
                movement.Stop();
                movement.ForceGroundSnap();
            }

            animationComponent.PlaySleep();
        }

        public void WakeUp()
        {
            if (animationComponent == null)
                return;

            animationComponent.PlayWakeUp();

            // For testing. Later unlock this using animation event after WakeUp animation ends.
            inputLocked = false;
        }

        public void Hit()
        {
            if (animationComponent == null)
                return;

            animationComponent.PlayHit();
        }

        public void Die()
        {
            if (animationComponent == null)
                return;

            if (animationComponent.IsInjuredDown)
            {
                animationComponent.PlayInjuredUp();
                return;
            }

            if (!animationComponent.CanPlayInjuredDown())
                return;

            if (movement != null)
                movement.Stop();

            animationComponent.PlayInjuredDown();
        }

        public void Eat()
        {
            if (inputLocked || animationComponent == null || animationComponent.IsEating)
                return;

            if (movement != null)
                movement.Stop();

            animationComponent.PlayEat();
        }

        public void Growl()
        {
            if (inputLocked || animationComponent == null)
                return;

            if (animationComponent.IsAttacking || animationComponent.IsEating || animationComponent.IsGrowling)
                return;

            animationComponent.StopEat();

            if (movement != null)
                movement.Stop();

            animationComponent.PlayGrowl();
        }

        public void Jump()
        {
            if (inputLocked || animationComponent == null)
                return;

            if (!animationComponent.CanJump())
                return;

            animationComponent.StopEat();
            animationComponent.PlayJump();
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;

            if (locked)
            {
                sprintToggled = false;

                if (movement != null)
                {
                    movement.Sprint(false);
                    movement.Stop();
                }
            }
        }

        private static void EnableAction(InputActionReference actionReference)
        {
            if (actionReference != null)
                actionReference.action.Enable();
        }

        private static void DisableAction(InputActionReference actionReference)
        {
            if (actionReference != null)
                actionReference.action.Disable();
        }

        private static void Register(InputActionReference actionReference, System.Action<InputAction.CallbackContext> callback)
        {
            if (actionReference == null)
                return;

            actionReference.action.Enable();
            actionReference.action.performed += callback;
        }

        private static void Unregister(InputActionReference actionReference, System.Action<InputAction.CallbackContext> callback)
        {
            if (actionReference == null)
                return;

            actionReference.action.performed -= callback;
            actionReference.action.Disable();
        }
    }
}
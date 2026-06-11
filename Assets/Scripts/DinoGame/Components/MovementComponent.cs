using UnityEngine;
using DinoGame.AI;
using DinoGame.Combat;
using DinoGame.Core;
using DinoGame.Data;
using DinoGame.Interfaces;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    public sealed class MovementComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Rigidbody rigidBody;
        [SerializeField] private bool useCharacterController = true;

        [Header("Ground / Gravity")]
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedStickForce = -2f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Movement")]
        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float deceleration = 4f;
        [SerializeField] private float directionSmoothTime = 0.28f;
        [SerializeField] private float turnSmoothTime = 0.32f;
        [SerializeField] private float maxTurnSpeed = 220f;
        [SerializeField] private float turnSlowdownAngle = 120f;
        [SerializeField] private float slideStopSpeed = 0.35f;
        [SerializeField] private float crouchMoveSpeed = 2.5f;
        [SerializeField] private float crouchAcceleration = 6f;

        [Header("Arc / Circular Locomotion (Player)")]
        [SerializeField] private bool enableArcLocomotion = true;
        [SerializeField] private float arcSteerAngleThreshold = 20f;
        [SerializeField, Range(0.2f, 1f)] private float arcSteerExitRatio = 0.55f;
        [SerializeField] private float arcTurnSpeed = 105f;
        [SerializeField] private float arcMaxSteerAngle = 70f;
        [SerializeField] private float arcBoneLeanMultiplier = 1.35f;

        [Header("Combat Separation")]
        [SerializeField] private bool enableHostileSeparation = true;
        [SerializeField, Min(0f)] private float separationStrength = 7f;
        [SerializeField, Min(0f)] private float separationPadding = 0.25f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float runJumpForce = 8.5f;
        [SerializeField] private float runJumpLandingSpeed = 12f;

        private Creature owner;
        private CreatureProfile profile;
        private StatusComponent status;
        private BoneTurnComponent boneTurn;
        private FootGroundingComponent footGrounding;

        private bool sprinting;
        private bool crouching;
        private bool jumping;
        private bool runJumpActive;
        private bool hasMoveInput;
        private Vector3 desiredMoveDirection;
        private Vector3 velocityDirection;
        private float currentSpeed;
        private float facingTurnAngle;
        private float leanAngle;
        private float turnRate;
        private float yawVelocity;

        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private bool movedThisFrame;
        private bool combatPositionLocked;
        private Vector3 combatLockPosition;
        private bool arcLocomotionActive;

        public Vector3 Velocity => horizontalVelocity;
        public bool IsArcLocomotion => arcLocomotionActive;
        public bool IsSprinting => sprinting && hasMoveInput && !crouching;
        public bool IsCrouching => crouching;
        public bool IsJumping => jumping;
        public bool IsGrounded { get; private set; }
        public float CurrentMoveSpeed => horizontalVelocity.magnitude;
        public float FacingTurnAngle => facingTurnAngle;
        public float LeanAngle => leanAngle;
        public float TurnRate => turnRate;
        public int MoveMode => !hasMoveInput && currentSpeed <= slideStopSpeed ? 0 : sprinting ? 2 : 1;

        public void Initialize(Creature creature, CreatureProfile creatureProfile, StatusComponent statusComponent)
        {
            owner = creature;
            profile = creatureProfile;
            status = statusComponent;

            characterController ??= GetComponent<CharacterController>();
            rigidBody ??= GetComponent<Rigidbody>();
            boneTurn ??= GetComponent<BoneTurnComponent>();
            footGrounding = GetComponent<FootGroundingComponent>();
            if (footGrounding == null)
                footGrounding = gameObject.AddComponent<FootGroundingComponent>();

            footGrounding.Bind(creature, groundMask);
            useCharacterController = characterController != null;
            velocityDirection = transform.forward;
        }

        private void FixedUpdate()
        {
            if (owner == null || !owner.IsAlive)
                return;

            if (combatPositionLocked)
            {
                HaltForCombat();
                RestoreCombatLockPosition();
                ApplyHostileSeparation(Time.fixedDeltaTime);
                return;
            }

            ApplyMovement(Time.fixedDeltaTime);
        }

        public void Tick(float deltaTime)
        {
            UpdateGrounded();
            ApplyGravity(deltaTime);

            if (!movedThisFrame)
                ApplyVerticalMovement(deltaTime);

            movedThisFrame = false;
        }

        public void Move(Vector3 direction)
        {
            if (owner == null || !owner.IsAlive)
            {
                Stop();
                return;
            }

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            desiredMoveDirection = direction;
            hasMoveInput = true;
        }

        public void Rotate(Vector3 direction)
        {
            Move(direction);
        }

        public void Sprint(bool enabled)
        {
            sprinting = enabled && !crouching;
        }

        public void SetCrouching(bool enabled)
        {
            if (jumping)
                enabled = false;

            if (crouching == enabled)
                return;

            crouching = enabled;

            if (!crouching)
                return;

            sprinting = false;
            currentSpeed = Mathf.Min(currentSpeed, crouchMoveSpeed);
        }

        public void BeginJump(bool runJump)
        {
            jumping = true;
            runJumpActive = runJump;
        }

        public void ApplyJumpImpulse()
        {
            if (!jumping)
                return;

            verticalVelocity = runJumpActive ? runJumpForce : jumpForce;
            IsGrounded = false;
        }

        public void EndJump()
        {
            if (runJumpActive && hasMoveInput)
                currentSpeed = Mathf.Max(currentSpeed, runJumpLandingSpeed);

            jumping = false;
            runJumpActive = false;

            if (IsGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedStickForce;
        }

        public void Stop()
        {
            hasMoveInput = false;
            facingTurnAngle = 0f;
            leanAngle = 0f;
            arcLocomotionActive = false;
        }

        public void HaltForCombat()
        {
            hasMoveInput = false;
            sprinting = false;
            currentSpeed = 0f;
            horizontalVelocity = Vector3.zero;
            facingTurnAngle = 0f;
            leanAngle = 0f;
            turnRate = 0f;
            arcLocomotionActive = false;
        }

        public void SnapToGround()
        {
            if (!TryGetGroundPosition(transform.position, out Vector3 groundedPosition))
                return;

            SetWorldPosition(groundedPosition);
        }

        public void BeginCombatPositionLock()
        {
            combatLockPosition = transform.position;
            combatPositionLocked = true;
            HaltForCombat();
        }

        public void EndCombatPositionLock() => combatPositionLocked = false;

        public void MaintainCombatPositionLock()
        {
            if (!combatPositionLocked)
                return;

            HaltForCombat();
            RestoreCombatLockPosition();
        }

        private bool TryGetGroundPosition(Vector3 fromPosition, out Vector3 groundedPosition)
        {
            groundedPosition = fromPosition;

            if (!Physics.Raycast(
                    fromPosition + Vector3.up * 3f,
                    Vector3.down,
                    out RaycastHit hit,
                    12f,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            float groundY = hit.point.y;

            if (useCharacterController && characterController != null)
            {
                groundedPosition.y = groundY - characterController.center.y + characterController.height * 0.5f;
                return true;
            }

            groundedPosition.y = groundY;
            return true;
        }

        private void SetWorldPosition(Vector3 position)
        {
            if (useCharacterController && characterController != null)
            {
                characterController.enabled = false;
                transform.position = position;
                characterController.enabled = true;
                return;
            }

            transform.position = position;
        }

        private void RestoreCombatLockPosition()
        {
            Vector3 position = transform.position;
            position.x = combatLockPosition.x;
            position.z = combatLockPosition.z;
            SetWorldPosition(position);
        }

        private void ApplyMovement(float deltaTime)
        {
            if (owner?.Animation != null && owner.Animation.IsAttacking && owner.Animation.AttackStopsMovement)
            {
                HaltForCombat();
            }

            if (jumping && !IsGrounded)
                sprinting = runJumpActive;

            float statusMultiplier = status != null ? status.MovementMultiplier : 1f;
            float woundedMultiplier = owner != null ? owner.GetWoundedSpeedMultiplier() : 1f;
            float maxSpeed = GetBaseSpeed() * statusMultiplier * woundedMultiplier;
            float targetSpeed = hasMoveInput ? maxSpeed : 0f;
            float referenceSpeed = Mathf.Max(maxSpeed, currentSpeed, 0.01f);
            float accel = targetSpeed > currentSpeed
                ? (crouching ? crouchAcceleration : acceleration)
                : deceleration;
            float speedDelta = accel * referenceSpeed * deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedDelta);

            if (hasMoveInput)
            {
                velocityDirection = SmoothDirection(
                    velocityDirection.sqrMagnitude > 0.001f ? velocityDirection : transform.forward,
                    desiredMoveDirection,
                    deltaTime);
            }
            else if (currentSpeed > slideStopSpeed)
            {
                velocityDirection = SmoothDirection(velocityDirection, velocityDirection, deltaTime);
                horizontalVelocity = velocityDirection * currentSpeed;
                leanAngle = Vector3.SignedAngle(transform.forward, velocityDirection, Vector3.up);

                if (boneTurn != null)
                    boneTurn.TickTurn(leanAngle, currentSpeed, deltaTime);

                MoveCharacter(deltaTime);
                return;
            }
            else
            {
                currentSpeed = 0f;
                turnRate = Mathf.Lerp(turnRate, 0f, deltaTime * 6f);
                facingTurnAngle = 0f;
                leanAngle = 0f;
                horizontalVelocity = Vector3.zero;

                if (boneTurn != null)
                    boneTurn.TickTurn(0f, 0f, deltaTime);

                return;
            }

            if (ShouldUseArcLocomotion())
            {
                ApplyArcLocomotion(deltaTime);
                MoveCharacter(deltaTime);
                ApplyHostileSeparation(deltaTime);
                return;
            }

            arcLocomotionActive = false;

            float turnPenalty = 1f;
            if (turnSlowdownAngle > 0.01f && hasMoveInput)
            {
                float angleToInput = Vector3.Angle(transform.forward, desiredMoveDirection);
                turnPenalty = Mathf.Clamp01(1f - angleToInput / turnSlowdownAngle * 0.55f);
            }

            horizontalVelocity = velocityDirection * (currentSpeed * turnPenalty);

            float targetYaw;
            if (hasMoveInput)
            {
                targetYaw = Mathf.Atan2(desiredMoveDirection.x, desiredMoveDirection.z) * Mathf.Rad2Deg;
            }
            else
            {
                targetYaw = transform.eulerAngles.y;
            }

            float previousYaw = transform.eulerAngles.y;
            float currentYaw = previousYaw;
            facingTurnAngle = Mathf.DeltaAngle(currentYaw, targetYaw);
            leanAngle = ComputeLeanAngle();

            float newYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                turnSmoothTime,
                maxTurnSpeed,
                deltaTime);

            turnRate = Mathf.DeltaAngle(previousYaw, newYaw) / deltaTime;
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

            if (boneTurn != null)
                boneTurn.TickTurn(leanAngle, currentSpeed, deltaTime);

            MoveCharacter(deltaTime);
            ApplyHostileSeparation(deltaTime);
        }

        private bool IsPlayerControlled()
        {
            return owner == null || owner.AI == null || !owner.AI.enabled;
        }

        private bool ShouldUseArcLocomotion()
        {
            if (!enableArcLocomotion || !IsPlayerControlled() || !hasMoveInput || crouching)
                return false;

            float steerAngle = Vector3.SignedAngle(transform.forward, desiredMoveDirection, Vector3.up);
            float enterThreshold = arcSteerAngleThreshold;
            float exitThreshold = arcSteerAngleThreshold * arcSteerExitRatio;

            if (arcLocomotionActive)
            {
                arcLocomotionActive = Mathf.Abs(steerAngle) >= exitThreshold;
                return arcLocomotionActive;
            }

            arcLocomotionActive = Mathf.Abs(steerAngle) >= enterThreshold;
            return arcLocomotionActive;
        }

        private void ApplyArcLocomotion(float deltaTime)
        {
            arcLocomotionActive = true;

            float steerAngle = Vector3.SignedAngle(transform.forward, desiredMoveDirection, Vector3.up);
            float steer01 = arcMaxSteerAngle > 0.01f
                ? Mathf.Clamp(steerAngle / arcMaxSteerAngle, -1f, 1f)
                : Mathf.Sign(steerAngle);

            velocityDirection = transform.forward;
            horizontalVelocity = transform.forward * currentSpeed;

            float previousYaw = transform.eulerAngles.y;
            float yawDelta = steer01 * arcTurnSpeed * deltaTime;
            float newYaw = previousYaw + yawDelta;

            turnRate = yawDelta / deltaTime;
            facingTurnAngle = steerAngle;
            leanAngle = steerAngle * arcBoneLeanMultiplier;

            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

            if (boneTurn != null)
                boneTurn.TickTurn(leanAngle, currentSpeed, deltaTime);
        }

        public bool IsBlockedByHostileSpacing(ITargetable target, float attackRange, float separationBuffer)
        {
            if (!enableHostileSeparation || owner == null || target == null)
                return false;

            if (!CombatSpacing.IsWithinMeleeRange(owner, target, attackRange))
                return false;

            return CombatSpacing.IsOverlapping(owner, target, separationBuffer, out _);
        }

        private void ApplyHostileSeparation(float deltaTime)
        {
            if (!enableHostileSeparation || owner == null || !owner.IsAlive)
                return;

            float selfRadius = CombatSpacing.GetBodyRadius(owner);
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                selfRadius * 4f,
                ~0,
                CombatPhysics.TargetQuery);

            Vector3 push = Vector3.zero;
            int count = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TargetableResolver.TryResolve(hits[i], out ITargetable other))
                    continue;

                if (!other.IsAlive || other.TargetTransform == transform || other.TeamId == owner.TeamId)
                    continue;

                Vector3 away = transform.position - other.TargetTransform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance < 0.001f)
                    continue;

                float minDistance = selfRadius + CombatSpacing.GetBodyRadius(other) + separationPadding;
                if (distance >= minDistance)
                    continue;

                float penetration = (minDistance - distance) / minDistance;
                push += away.normalized * penetration;
                count++;
            }

            if (count == 0)
                return;

            push = (push / count).normalized * separationStrength * deltaTime;

            if (useCharacterController && characterController != null)
                characterController.Move(push);
            else
                transform.position += push;
        }

        private float ComputeLeanAngle()
        {
            if (currentSpeed < 0.15f)
                return 0f;

            float inputAngle = hasMoveInput
                ? Vector3.SignedAngle(transform.forward, desiredMoveDirection, Vector3.up)
                : 0f;

            float driftAngle = Vector3.SignedAngle(transform.forward, velocityDirection, Vector3.up);

            if (!hasMoveInput)
                return driftAngle;

            if (Mathf.Sign(inputAngle) == Mathf.Sign(driftAngle) || Mathf.Abs(driftAngle) < 2f)
                return Mathf.Abs(inputAngle) >= Mathf.Abs(driftAngle) ? inputAngle : driftAngle;

            return Mathf.Abs(inputAngle) >= Mathf.Abs(driftAngle) ? inputAngle : driftAngle;
        }

        private Vector3 SmoothDirection(Vector3 current, Vector3 target, float deltaTime)
        {
            if (target.sqrMagnitude < 0.001f)
                return current;

            target.Normalize();

            if (current.sqrMagnitude < 0.001f)
                return target;

            current.Normalize();

            float smoothFactor = 1f - Mathf.Exp(-deltaTime / directionSmoothTime);
            return Vector3.Slerp(current, target, smoothFactor).normalized;
        }

        private float GetBaseSpeed()
        {
            if (jumping && !IsGrounded)
            {
                if (profile == null)
                    return runJumpActive ? runJumpLandingSpeed : 0f;

                return runJumpActive ? profile.sprintSpeed : profile.moveSpeed;
            }

            if (crouching)
                return crouchMoveSpeed;

            if (profile == null)
                return 0f;

            return sprinting ? profile.sprintSpeed : profile.moveSpeed;
        }

        private void MoveCharacter(float deltaTime)
        {
            movedThisFrame = true;
            

            if (useCharacterController && characterController != null)
            {
                Vector3 motion = horizontalVelocity;
                motion.y = verticalVelocity;
                characterController.Move(motion * deltaTime);
            }
            else if (rigidBody != null && !rigidBody.isKinematic)
            {
                Vector3 nextPosition = rigidBody.position + horizontalVelocity * deltaTime;
                rigidBody.MovePosition(nextPosition);
            }
            else
            {
                transform.position += horizontalVelocity * deltaTime;
            }
        }

        private void UpdateGrounded()
        {
            if (characterController != null)
            {
                IsGrounded = characterController.isGrounded;
                return;
            }

            Vector3 checkPosition = groundCheckPoint != null
                ? groundCheckPoint.position
                : transform.position + Vector3.up * 0.1f;

            IsGrounded = Physics.CheckSphere(
                checkPosition,
                groundCheckRadius,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }

        public void ForceGroundSnap(float maxDistance = 10f)
        {
            Vector3 origin = transform.position + Vector3.up * 2f;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
                return;

            float yOffset = 0f;

            if (characterController != null)
                yOffset = characterController.height * 0.5f - characterController.center.y;

            Vector3 targetPosition = transform.position;
            targetPosition.y = hit.point.y + yOffset;

            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = targetPosition;
                characterController.enabled = true;
            }
            else if (rigidBody != null)
            {
                rigidBody.position = targetPosition;
                rigidBody.linearVelocity = Vector3.zero;
            }
            else
            {
                transform.position = targetPosition;
            }

            verticalVelocity = groundedStickForce;
            IsGrounded = true;
        }

        private void ApplyGravity(float deltaTime)
        {
            if (IsGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedStickForce;
            else
                verticalVelocity += gravity * deltaTime;
        }

        private void ApplyVerticalMovement(float deltaTime)
        {
            if (useCharacterController && characterController != null)
            {
                Vector3 verticalMotion = Vector3.up * verticalVelocity;
                characterController.Move(verticalMotion * deltaTime);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 checkPosition = groundCheckPoint != null
                ? groundCheckPoint.position
                : transform.position + Vector3.up * 0.1f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }
#endif
    }
}

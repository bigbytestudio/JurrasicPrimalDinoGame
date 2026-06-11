using UnityEngine;
using DinoGame.Core;

namespace DinoGame.Components
{
    /// <summary>
    /// Aligns the creature root to planted foot bones after animation so feet meet terrain.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1200)]
    public sealed class FootGroundingComponent : MonoBehaviour
    {
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0f)] private float footGroundOffset = 0.04f;
        [SerializeField, Min(0.01f)] private float maxVerticalAdjustPerFrame = 0.16f;
        [SerializeField, Min(0.05f)] private float airborneFootThreshold = 0.22f;
        [SerializeField] private bool configureHitCollidersAsTriggers = true;

        private Creature owner;
        private MovementComponent movement;
        private AnimationComponent animationComponent;
        private CharacterController characterController;

        public void Bind(Creature creature, LayerMask movementGroundMask)
        {
            owner = creature;
            movement = creature != null ? creature.Movement : null;
            animationComponent = creature != null ? creature.Animation : null;
            characterController = GetComponent<CharacterController>();
            groundMask = movementGroundMask;

            if (leftFoot == null || rightFoot == null)
                AutoAssignFeet();

            if (configureHitCollidersAsTriggers)
                ConfigureHitCollidersAsTriggers();
        }

        private void LateUpdate()
        {
            ApplyFootGrounding();
        }

        private void ApplyFootGrounding()
        {
            if (owner == null || movement == null)
                return;

            if (animationComponent != null && animationComponent.IsPlayingDeath)
                return;

            if (movement.IsJumping || !movement.IsGrounded)
                return;

            if (animationComponent != null && animationComponent.IsJumping)
                return;

            if (!TryComputeFootCorrection(out float correction))
                return;

            float step = Mathf.Clamp(correction, -maxVerticalAdjustPerFrame, maxVerticalAdjustPerFrame);
            if (Mathf.Abs(step) < 0.001f)
                return;

            Vector3 position = transform.position;
            position.y += step;
            SetWorldPosition(position);
        }

        private bool TryComputeFootCorrection(out float correction)
        {
            correction = 0f;
            int sampleCount = 0;
            float total = 0f;

            if (TrySampleFoot(leftFoot, out float leftCorrection))
            {
                total += leftCorrection;
                sampleCount++;
            }

            if (TrySampleFoot(rightFoot, out float rightCorrection))
            {
                total += rightCorrection;
                sampleCount++;
            }

            if (sampleCount == 0)
                return false;

            correction = total / sampleCount;
            return true;
        }

        private bool TrySampleFoot(Transform foot, out float correction)
        {
            correction = 0f;
            if (foot == null)
                return false;

            Vector3 footPosition = foot.position;
            if (!Physics.Raycast(
                    footPosition + Vector3.up * 0.85f,
                    Vector3.down,
                    out RaycastHit hit,
                    2.5f,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            float gap = footPosition.y - hit.point.y - footGroundOffset;
            if (gap > airborneFootThreshold)
                return false;

            correction = -gap;
            return true;
        }

        private void SetWorldPosition(Vector3 position)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = position;
                characterController.enabled = true;
                return;
            }

            transform.position = position;
        }

        private void AutoAssignFeet()
        {
            Transform[] bones = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < bones.Length; i++)
            {
                string name = bones[i].name.ToLowerInvariant();
                if (leftFoot == null && (name == "left foot0" || name == "left_foot0"))
                    leftFoot = bones[i];
                else if (rightFoot == null && (name == "right foot0" || name == "right_foot0"))
                    rightFoot = bones[i];
            }
        }

        private void ConfigureHitCollidersAsTriggers()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider is CharacterController)
                    continue;

                collider.isTrigger = true;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (leftFoot == null || rightFoot == null)
                AutoAssignFeet();
        }
#endif
    }
}

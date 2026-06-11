using UnityEngine;
using DinoGame.Core;
using DinoGame.Components;

namespace DinoGame.Strategies.Movement
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/Movement/Swim", fileName = "SwimMovement")]
    public sealed class SwimMovementStrategy : MovementStrategy
    {
        [SerializeField] private float swimSpeedMultiplier = 0.65f;
        [SerializeField] private float verticalDamping = 0.85f;

        public override void Move(Creature owner, MovementComponent movement, Vector3 direction, float speed)
        {
            if (owner == null || movement == null)
                return;

            direction.y = 0f;

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            owner.Rotate(direction);

            float finalSpeed = speed * swimSpeedMultiplier;
            Vector3 delta = direction * finalSpeed * verticalDamping;

            CharacterController controller = owner.GetComponent<CharacterController>();
            Rigidbody rb = owner.GetComponent<Rigidbody>();

            if (controller != null)
            {
                controller.Move(delta * Time.deltaTime);
            }
            else if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(rb.position + delta * Time.deltaTime);
            }
            else
            {
                owner.transform.position += delta * Time.deltaTime;
            }
        }
    }
}
using UnityEngine;
using DinoGame.Core;
using DinoGame.Components;

namespace DinoGame.Strategies.Movement
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/Movement/Ground", fileName = "GroundMovement")]
    public sealed class GroundMovementStrategy : MovementStrategy
    {
        public override void Move(Creature owner, MovementComponent movement, Vector3 direction, float speed)
        {
            if (owner == null || movement == null)
                return;

            direction.y = 0f;

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            CharacterController controller = owner.GetComponent<CharacterController>();
            Rigidbody rb = owner.GetComponent<Rigidbody>();

            Vector3 horizontalVelocity = direction * speed;

            if (controller != null)
            {
                Vector3 motion = horizontalVelocity;
                motion.y = movement.IsGrounded ? -2f : -25f;

                controller.Move(motion * Time.deltaTime);
            }
            else if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(rb.position + horizontalVelocity * Time.deltaTime);
            }
            else
            {
                owner.transform.position += horizontalVelocity * Time.deltaTime;
            }
        }
    }
}
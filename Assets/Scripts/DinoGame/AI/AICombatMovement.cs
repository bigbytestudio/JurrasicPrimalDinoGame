using UnityEngine;
using DinoGame.Combat;
using DinoGame.Components;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.AI
{
    internal static class AICombatMovement
    {
        public static bool HandleCombatRange(Creature owner, AIComponent ai, ITargetable target, float attackRange, float separationBuffer)
        {
            if (owner == null || ai == null || target == null || attackRange <= 0f)
                return false;

            float distance = CombatSpacing.GetFlatDistance(owner, target);
            if (distance > attackRange * 1.15f)
                return false;

            if (owner.Animation == null || !owner.Animation.IsAttacking)
                ai.Behavior.Current = AIState.Attack;

            owner.Sprint(false);

            Vector3 faceDirection = CombatSpacing.GetApproachDirection(owner, target);

            if (CombatSpacing.IsOverlapping(owner, target, separationBuffer, out _))
            {
                owner.Move(CombatSpacing.GetBackOffDirection(owner, target));
                owner.Rotate(faceDirection);
                TryAttack(owner, target);
                return true;
            }

            if (CombatSpacing.ShouldApproachForMelee(owner, target, attackRange))
            {
                owner.Move(faceDirection);
                owner.Rotate(faceDirection);
                return true;
            }

            owner.Movement?.HaltForCombat();
            owner.Rotate(faceDirection);
            TryAttack(owner, target);
            return true;
        }

        public static bool ShouldSprintToTarget(Creature owner, ITargetable target, float sprintDistance, float attackRange)
        {
            float distance = CombatSpacing.GetFlatDistance(owner, target);
            return distance > sprintDistance && distance > attackRange * 1.6f;
        }

        private static void TryAttack(Creature owner, ITargetable target)
        {
            bool isAttacking = owner.Animation != null && owner.Animation.IsAttacking;
            if (isAttacking || !owner.CanAttack(target))
                return;

            owner.TryCombatAttack(target);
        }
    }
}

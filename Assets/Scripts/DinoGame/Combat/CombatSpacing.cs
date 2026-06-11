using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Combat
{
    public static class CombatSpacing
    {
        public static float GetBodyRadius(Creature creature)
        {
            if (creature == null)
                return 0.55f;

            if (creature.TryGetComponent<CharacterController>(out CharacterController controller))
                return Mathf.Max(0.35f, controller.radius);

            return 0.55f;
        }

        public static float GetBodyRadius(ITargetable target)
        {
            if (target?.TargetTransform == null)
                return 0.55f;

            if (target.TargetTransform.TryGetComponent<CharacterController>(out CharacterController controller))
                return Mathf.Max(0.35f, controller.radius);

            return 0.55f;
        }

        public static float GetMinSeparationDistance(Creature self, ITargetable target, float extraBuffer = 0.35f)
        {
            return GetBodyRadius(self) + GetBodyRadius(target) + extraBuffer;
        }

        public static float GetFlatDistance(Creature self, ITargetable target)
        {
            if (self == null || target?.TargetTransform == null)
                return float.MaxValue;

            Vector3 offset = target.TargetTransform.position - self.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        public static Vector3 GetFlatOffset(Creature self, ITargetable target)
        {
            if (self == null || target?.TargetTransform == null)
                return Vector3.zero;

            Vector3 offset = target.TargetTransform.position - self.transform.position;
            offset.y = 0f;
            return offset;
        }

        public static bool IsOverlapping(Creature self, ITargetable target, float extraBuffer, out float penetration)
        {
            penetration = 0f;
            if (self == null || target == null || !target.IsAlive)
                return false;

            float distance = GetFlatDistance(self, target);
            float minSeparation = GetMinSeparationDistance(self, target, extraBuffer);
            if (distance >= minSeparation)
                return false;

            penetration = minSeparation - distance;
            return true;
        }

        public static bool IsWithinMeleeRange(Creature self, ITargetable target, float attackRange)
        {
            if (self == null || target == null)
                return false;

            return GetFlatDistance(self, target) <= attackRange;
        }

        public static bool ShouldApproachForMelee(Creature self, ITargetable target, float attackRange)
        {
            if (self == null || target == null)
                return false;

            return GetFlatDistance(self, target) > attackRange;
        }

        public static Vector3 GetBackOffDirection(Creature self, ITargetable target)
        {
            Vector3 away = self.transform.position - target.TargetTransform.position;
            away.y = 0f;
            return away.sqrMagnitude > 0.001f ? away.normalized : -self.transform.forward;
        }

        public static Vector3 GetApproachDirection(Creature self, ITargetable target)
        {
            Vector3 toTarget = target.TargetTransform.position - self.transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : self.transform.forward;
        }
    }
}

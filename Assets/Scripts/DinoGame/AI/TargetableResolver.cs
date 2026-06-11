using UnityEngine;
using DinoGame.Interfaces;

namespace DinoGame.AI
{
    internal static class TargetableResolver
    {
        public static bool TryResolve(Collider collider, out ITargetable target)
        {
            target = null;
            if (collider == null)
                return false;

            if (collider.TryGetComponent(out target))
                return true;

            target = collider.GetComponentInParent<ITargetable>();
            return target != null;
        }
    }
}

using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.Attack
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/Attack/Charge", fileName = "ChargeAttack")]
    public sealed class ChargeAttackStrategy : AttackStrategy
    {
        [SerializeField, Min(0f)] private float impulse = 4f;
        public override void Execute(Creature owner, ITargetable target)
        {
            if (!CanAttack(owner, target)) return;
            DamageTarget(owner, target, Damage, StatusEffect);
            Vector3 away = (target.TargetTransform.position - owner.transform.position).normalized;
            if (target.TargetTransform.TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
                rb.AddForce(away * impulse, ForceMode.VelocityChange);
        }
    }
}

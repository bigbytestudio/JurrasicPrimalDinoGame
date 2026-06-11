using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;
using DinoGame.Data;

namespace DinoGame.Strategies.Attack
{
    public abstract class AttackStrategy : ScriptableObject
    {
        [SerializeField] private string id = "attack";
        [SerializeField, Min(0f)] private float range = 2.2f;
        [SerializeField, Min(0f)] private float damage = 12f;
        [SerializeField, Min(0f)] private float cooldown = 1f;
        [SerializeField] private StatusEffectData statusEffect;

        public string Id => id;
        public float Range => range;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public StatusEffectData StatusEffect => statusEffect;

        public virtual bool CanAttack(Creature owner, ITargetable target)
        {
            if (owner == null || target == null || !owner.IsAlive || !target.IsAlive) return false;
            return Vector3.Distance(owner.transform.position, target.TargetTransform.position) <= range;
        }

        public abstract void Execute(Creature owner, ITargetable target);

        protected static void DamageTarget(Creature owner, ITargetable target, float damage, StatusEffectData statusEffect)
        {
            if (target is IDamageable damageable) damageable.TakeDamage(damage, owner.gameObject);
            if (statusEffect != null && target.TargetTransform.TryGetComponent(out DinoGame.Components.StatusComponent status))
                status.AddEffect(statusEffect);
        }
    }
}

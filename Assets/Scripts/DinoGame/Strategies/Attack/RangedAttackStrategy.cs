using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.Attack
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/Attack/Ranged", fileName = "RangedAttack")]
    public sealed class RangedAttackStrategy : AttackStrategy
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Min(0f)] private float projectileSpeed = 20f;

        public override void Execute(Creature owner, ITargetable target)
        {
            if (!CanAttack(owner, target)) return;
            if (projectilePrefab == null)
            {
                DamageTarget(owner, target, Damage, StatusEffect);
                return;
            }

            Transform point = spawnPoint != null ? spawnPoint : owner.transform;
            GameObject projectile = Instantiate(projectilePrefab, point.position + owner.transform.forward, Quaternion.LookRotation(owner.transform.forward));
            if (projectile.TryGetComponent(out Rigidbody rb)) rb.linearVelocity = owner.transform.forward * projectileSpeed;
            if (projectile.TryGetComponent(out ProjectileDamage projectileDamage)) projectileDamage.Initialize(owner.gameObject, Damage, StatusEffect);
        }
    }

    public sealed class ProjectileDamage : MonoBehaviour
    {
        private GameObject source;
        private float damage;
        private DinoGame.Data.StatusEffectData effect;
        public void Initialize(GameObject sourceObject, float amount, DinoGame.Data.StatusEffectData statusEffect)
        {
            source = sourceObject;
            damage = amount;
            effect = statusEffect;
            Destroy(gameObject, 5f);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (source != null && other.gameObject == source) return;
            if (other.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(damage, source);
            if (effect != null && other.TryGetComponent(out DinoGame.Components.StatusComponent status)) status.AddEffect(effect);
            Destroy(gameObject);
        }
    }
}

using System;
using UnityEngine;
using DinoGame.Core;
using DinoGame.Data;
using DinoGame.Interfaces;
using DinoGame.Systems;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour, IDamageable, IHealable
    {
        public event Action<float, GameObject> Damaged;
        public event Action Died;

        [SerializeField] private float currentHealth = 100f;
        private float maxHealth = 100f;
        private Creature owner;

        public bool IsAlive => currentHealth > 0f;

        public void Initialize(Creature creature, CreatureProfile profile)
        {
            owner = creature;
            maxHealth = Mathf.Max(1f, profile != null ? profile.maxHealth : 100f);
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Damaged?.Invoke(amount, source);
            GameEventBus.RaiseDamage(owner, amount, source);
            if (currentHealth <= 0f)
            {
                Died?.Invoke();
                GameEventBus.RaiseDeath(owner, source);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        public float GetHealth01() => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    }
}

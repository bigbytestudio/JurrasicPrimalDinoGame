using System.Collections.Generic;
using UnityEngine;
using DinoGame.Core;
using DinoGame.Data;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    public sealed class StatusComponent : MonoBehaviour
    {
        private sealed class ActiveStatus
        {
            public StatusEffectData Data;
            public float Remaining;
            public float NextTick;
        }

        private readonly List<ActiveStatus> active = new();
        private Creature owner;
        public float MovementMultiplier { get; private set; } = 1f;

        public void Initialize(Creature creature)
        {
            owner = creature;
            RecalculateModifiers();
        }

        public void AddEffect(StatusEffectData data)
        {
            if (data == null) return;
            ActiveStatus existing = active.Find(s => s.Data != null && s.Data.type == data.type);
            if (existing != null)
            {
                existing.Data = data;
                existing.Remaining = data.duration;
                existing.NextTick = 0f;
            }
            else
            {
                active.Add(new ActiveStatus { Data = data, Remaining = data.duration });
            }
            RecalculateModifiers();
        }

        public void Tick(float deltaTime)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ActiveStatus status = active[i];
                status.Remaining -= deltaTime;
                status.NextTick -= deltaTime;
                if (status.Data != null && status.Data.tickDamage > 0f && status.NextTick <= 0f)
                {
                    status.NextTick = Mathf.Max(0.1f, status.Data.tickRate);
                    owner.TakeDamage(status.Data.tickDamage, gameObject);
                }
                if (status.Remaining <= 0f) active.RemoveAt(i);
            }
            RecalculateModifiers();
        }

        private void RecalculateModifiers()
        {
            float multiplier = 1f;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Data != null) multiplier *= active[i].Data.movementMultiplier;
            MovementMultiplier = Mathf.Clamp(multiplier, 0.05f, 2f);
        }

        public void Dispose()
        {
            active.Clear();
            MovementMultiplier = 1f;
        }
    }
}

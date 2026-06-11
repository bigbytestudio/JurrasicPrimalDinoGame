using System.Collections.Generic;
using UnityEngine;
using DinoGame.AI;
using DinoGame.Core;
using DinoGame.Data;
using DinoGame.Interfaces;
using DinoGame.Strategies.Attack;

namespace DinoGame.Components
{
    [DisallowMultipleComponent]
    public sealed class CombatComponent : MonoBehaviour
    {
        [SerializeField] private LayerMask meleeHitMask = ~0;

        private readonly Dictionary<string, float> nextUseTimeById = new();
        private Creature owner;
        private CreatureProfile profile;
        private AttackStrategy[] strategies = System.Array.Empty<AttackStrategy>();
        private ITargetable pendingMeleeTarget;
        private ITargetable attackFocusTarget;

        public void Initialize(Creature creature, CreatureProfile creatureProfile)
        {
            owner = creature;
            profile = creatureProfile;
            strategies = profile != null && profile.attackStrategies != null ? profile.attackStrategies : System.Array.Empty<AttackStrategy>();
        }

        public float GetMeleeRange()
        {
            float best = 0f;
            for (int i = 0; i < strategies.Length; i++)
            {
                AttackStrategy strategy = strategies[i];
                if (strategy == null)
                    continue;

                best = Mathf.Max(best, strategy.Range);
            }

            return best > 0f ? best : 2.2f;
        }

        public bool CanAttack(ITargetable target)
        {
            if (target == null || owner == null || !owner.IsAlive || !target.IsAlive) return false;
            for (int i = 0; i < strategies.Length; i++)
                if (strategies[i] != null && strategies[i].CanAttack(owner, target)) return true;
            return false;
        }

        public ITargetable AttackFocusTarget => attackFocusTarget;

        public void SetPendingMeleeTarget(ITargetable target)
        {
            pendingMeleeTarget = target;

            if (target != null && target.IsAlive)
                attackFocusTarget = target;
        }

        public void ClearAttackFocus() => attackFocusTarget = null;

        public ITargetable ResolveAttackFocus()
        {
            if (attackFocusTarget != null && attackFocusTarget.IsAlive)
                return attackFocusTarget;

            return FindNearestHostileInMeleeRange();
        }

        public ITargetable FindNearestHostileInMeleeRange()
        {
            ITargetable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < strategies.Length; i++)
            {
                AttackStrategy strategy = strategies[i];
                if (strategy == null)
                    continue;

                float range = strategy.Range;
                Collider[] hits = Physics.OverlapSphere(
                    owner.transform.position,
                    range,
                    meleeHitMask,
                    CombatPhysics.TargetQuery);

                for (int h = 0; h < hits.Length; h++)
                {
                    if (!TargetableResolver.TryResolve(hits[h], out ITargetable candidate))
                        continue;

                    if (!IsHostileTarget(candidate) || !strategy.CanAttack(owner, candidate))
                        continue;

                    float sqr = (candidate.TargetTransform.position - owner.transform.position).sqrMagnitude;
                    if (sqr >= bestSqr)
                        continue;

                    best = candidate;
                    bestSqr = sqr;
                }
            }

            return best;
        }

        public void Attack(ITargetable target)
        {
            if (target == null) return;
            for (int i = 0; i < strategies.Length; i++)
            {
                AttackStrategy strategy = strategies[i];
                if (strategy == null || !strategy.CanAttack(owner, target)) continue;
                if (IsOnCooldown(strategy.Id)) continue;
                strategy.Execute(owner, target);
                SetCooldown(strategy.Id, strategy.Cooldown);
                return;
            }
        }

        public void ApplyMeleeHit()
        {
            if (owner == null || !owner.IsAlive || strategies.Length == 0)
                return;

            if (pendingMeleeTarget != null && pendingMeleeTarget.IsAlive && TryAttackTarget(pendingMeleeTarget))
            {
                pendingMeleeTarget = null;
                return;
            }

            pendingMeleeTarget = null;
            TryAttackNearestHostileInRange();
        }

        private bool TryAttackTarget(ITargetable target)
        {
            for (int i = 0; i < strategies.Length; i++)
            {
                AttackStrategy strategy = strategies[i];
                if (strategy == null || !strategy.CanAttack(owner, target)) continue;
                if (IsOnCooldown(strategy.Id)) return false;
                strategy.Execute(owner, target);
                SetCooldown(strategy.Id, strategy.Cooldown);
                return true;
            }

            return false;
        }

        private void TryAttackNearestHostileInRange()
        {
            ITargetable best = FindNearestHostileInMeleeRange();
            if (best == null)
                return;

            for (int i = 0; i < strategies.Length; i++)
            {
                AttackStrategy strategy = strategies[i];
                if (strategy == null || IsOnCooldown(strategy.Id))
                    continue;

                if (!strategy.CanAttack(owner, best))
                    continue;

                strategy.Execute(owner, best);
                SetCooldown(strategy.Id, strategy.Cooldown);
                return;
            }
        }

        private bool IsHostileTarget(ITargetable target)
        {
            if (target == null || !target.IsAlive || target.TargetTransform == owner.transform)
                return false;

            return target.TeamId != owner.TeamId;
        }

        public void UseAbility(string abilityId, ITargetable target)
        {
            if (string.IsNullOrWhiteSpace(abilityId) || target == null) return;
            for (int i = 0; i < strategies.Length; i++)
            {
                AttackStrategy strategy = strategies[i];
                if (strategy == null || strategy.Id != abilityId) continue;
                if (!strategy.CanAttack(owner, target) || IsOnCooldown(strategy.Id)) return;
                strategy.Execute(owner, target);
                SetCooldown(strategy.Id, strategy.Cooldown);
                return;
            }
        }

        private bool IsOnCooldown(string id) => nextUseTimeById.TryGetValue(id, out float nextTime) && Time.time < nextTime;
        private void SetCooldown(string id, float cooldown) => nextUseTimeById[id] = Time.time + Mathf.Max(0f, cooldown);
        public void Dispose()
        {
            nextUseTimeById.Clear();
            pendingMeleeTarget = null;
            attackFocusTarget = null;
        }
    }
}

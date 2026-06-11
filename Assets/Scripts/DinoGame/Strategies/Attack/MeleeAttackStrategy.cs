using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.Attack
{
    [CreateAssetMenu(menuName = "Dino Game/Strategies/Attack/Melee", fileName = "MeleeAttack")]
    public sealed class MeleeAttackStrategy : AttackStrategy
    {
        public override void Execute(Creature owner, ITargetable target)
        {
            if (!CanAttack(owner, target)) return;
            DamageTarget(owner, target, Damage, StatusEffect);
        }
    }
}

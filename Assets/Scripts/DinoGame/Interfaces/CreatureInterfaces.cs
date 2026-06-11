using UnityEngine;

namespace DinoGame.Interfaces
{
     public interface IMovable
    {
        void Move(Vector3 direction);
        void Stop();
        void Rotate(Vector3 direction);
        void Sprint(bool enabled);
    }

    public interface IAttackable
    {
        bool CanAttack(ITargetable target);
        void Attack(ITargetable target);
        void UseAbility(string abilityId, ITargetable target);
    }

    public interface ITargetable
    {
        Transform TargetTransform { get; }
        bool IsAlive { get; }
        int TeamId { get; }
        bool CanSee(ITargetable target);
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount, GameObject source);
    }

    public interface IHealable
    {
        void Heal(float amount);
        float GetHealth01();
    }

    public interface IInteractable
    {
        bool CanInteract(GameObject actor);
        void Interact(GameObject actor);
    }
}

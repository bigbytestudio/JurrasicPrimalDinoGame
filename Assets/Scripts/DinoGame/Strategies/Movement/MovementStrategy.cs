using UnityEngine;
using DinoGame.Core;
using DinoGame.Components;

namespace DinoGame.Strategies.Movement
{
    public abstract class MovementStrategy : ScriptableObject
    {
        public abstract void Move(Creature owner, MovementComponent movement, Vector3 direction, float speed);
    }
}

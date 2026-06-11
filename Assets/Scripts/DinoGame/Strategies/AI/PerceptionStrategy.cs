using UnityEngine;
using DinoGame.Core;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.AI
{
    public abstract class PerceptionStrategy : ScriptableObject
    {
        public abstract bool CanSee(Creature owner, ITargetable target, float radius, float fieldOfView);
    }
}

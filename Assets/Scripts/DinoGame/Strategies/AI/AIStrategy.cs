using UnityEngine;
using DinoGame.Core;
using DinoGame.Components;
using DinoGame.Interfaces;

namespace DinoGame.Strategies.AI
{
    public abstract class AIStrategy : ScriptableObject
    {
        public abstract ITargetable Tick(Creature owner, AIComponent ai, ITargetable currentTarget);
    }
}

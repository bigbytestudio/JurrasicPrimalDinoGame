using System;
using UnityEngine;
using DinoGame.Core;

namespace DinoGame.Systems
{
    public static class GameEventBus
    {
        public static event Action<Creature, float, GameObject> DamageTaken;
        public static event Action<Creature, GameObject> CreatureDied;
        public static event Action<Creature> CreatureSpawned;

        public static void RaiseDamage(Creature creature, float amount, GameObject source) => DamageTaken?.Invoke(creature, amount, source);
        public static void RaiseDeath(Creature creature, GameObject source) => CreatureDied?.Invoke(creature, source);
        public static void RaiseSpawn(Creature creature) => CreatureSpawned?.Invoke(creature);

        public static void Clear()
        {
            DamageTaken = null;
            CreatureDied = null;
            CreatureSpawned = null;
        }
    }
}

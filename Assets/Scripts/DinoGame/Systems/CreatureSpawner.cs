using System.Collections.Generic;
using UnityEngine;
using DinoGame.Core;
using DinoGame.Data;

namespace DinoGame.Systems
{
    public static class CreatureSpawner
    {
        private static readonly Dictionary<int, Stack<Creature>> Pool = new();

        public static Creature Spawn(
            CreatureProfile profile,
            Vector3 position,
            Quaternion rotation,
            TeamType? teamOverride = null,
            bool enableAI = true)
        {
            if (profile == null || profile.prefab == null)
                return null;

            Creature creature = TryTakeFromPool(profile) ?? CreateInstance(profile);
            Transform creatureTransform = creature.transform;
            creatureTransform.SetPositionAndRotation(position, rotation);

            if (!creature.gameObject.activeSelf)
                creature.gameObject.SetActive(true);

            creature.InjectProfile(profile);

            if (teamOverride.HasValue)
                creature.SetTeam(teamOverride.Value);

            if (creature.AI != null)
                creature.AI.enabled = enableAI;

            GameEventBus.RaiseSpawn(creature);
            return creature;
        }

        public static void Despawn(Creature creature, CreatureProfile profile, bool usePool)
        {
            if (creature == null)
                return;

            creature.Dispose();

            if (usePool && profile != null && profile.prefab != null)
            {
                creature.gameObject.SetActive(false);
                ReturnToPool(creature, profile);
                return;
            }

            Object.Destroy(creature.gameObject);
        }

        private static Creature TryTakeFromPool(CreatureProfile profile)
        {
            int key = profile.prefab.GetInstanceID();
            if (!Pool.TryGetValue(key, out Stack<Creature> stack) || stack.Count == 0)
                return null;

            return stack.Pop();
        }

        private static Creature CreateInstance(CreatureProfile profile)
        {
            GameObject instance = Object.Instantiate(profile.prefab);
            if (!instance.TryGetComponent(out Creature creature))
                creature = instance.AddComponent<DinoCreature>();

            return creature;
        }

        private static void ReturnToPool(Creature creature, CreatureProfile profile)
        {
            int key = profile.prefab.GetInstanceID();
            if (!Pool.TryGetValue(key, out Stack<Creature> stack))
            {
                stack = new Stack<Creature>();
                Pool[key] = stack;
            }

            stack.Push(creature);
        }

        public static void ClearPool()
        {
            foreach (Stack<Creature> stack in Pool.Values)
            {
                while (stack.Count > 0)
                {
                    Creature creature = stack.Pop();
                    if (creature != null)
                        Object.Destroy(creature.gameObject);
                }
            }

            Pool.Clear();
        }
    }
}

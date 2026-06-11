using UnityEngine;
using DinoGame.Data;

namespace DinoGame.AI
{
    internal static class AIPatrolPace
    {
        public static void RollForNextLeg(AIBehaviorState state, AIConfig config, float fallbackRunChance)
        {
            if (state == null)
                return;

            float runChance = config != null ? config.patrolRunChance : fallbackRunChance;
            state.PatrolUseRun = Random.value < runChance;
            state.PatrolPaceRolled = true;
        }

        public static void EnsurePaceRolled(AIBehaviorState state, AIConfig config, float fallbackRunChance)
        {
            if (state == null || state.PatrolPaceRolled)
                return;

            RollForNextLeg(state, config, fallbackRunChance);
        }

        public static void ResetLeg(AIBehaviorState state)
        {
            if (state == null)
                return;

            state.PatrolPaceRolled = false;
        }
    }
}

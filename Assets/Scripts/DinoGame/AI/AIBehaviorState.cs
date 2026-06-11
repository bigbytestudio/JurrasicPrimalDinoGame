using UnityEngine;

namespace DinoGame.AI
{
    public sealed class AIBehaviorState
    {
        public AIState Current = AIState.Patrol;
        public int PatrolIndex;
        public float WaitTimer;
        public Vector3 HomePosition;
        public float HomeYaw;
        public bool GrowlPlayedForCurrentTarget;
        public float ChaseTimer;
        public bool PatrolUseRun;
        public bool PatrolPaceRolled;
        public bool ChaseSprint;
        public bool HasAggro;
        public float LastSeenTargetTime;

        public void MarkTargetSeen() => LastSeenTargetTime = Time.time;

        public void ClearAggro()
        {
            HasAggro = false;
            GrowlPlayedForCurrentTarget = false;
            ChaseTimer = 0f;
        }
    }
}

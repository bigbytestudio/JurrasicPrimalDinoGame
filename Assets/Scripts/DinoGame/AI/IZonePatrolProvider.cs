using UnityEngine;

namespace DinoGame.AI
{
    public interface IZonePatrolProvider
    {
        bool HasPatrolPoints { get; }
        int PatrolPointCount { get; }
        float PointReachRadius { get; }
        Vector3 GetPatrolPoint(int index);
        int GetRandomPatrolIndex(int excludeIndex = -1);
    }
}

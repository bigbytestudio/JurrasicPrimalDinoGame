using UnityEngine;

namespace DinoGame.AI
{
    /// <summary>
    /// Optional patrol path for AI creatures. Place waypoints as child transforms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AIPatrolRoute : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0.1f)] private float pointReachRadius = 1.25f;
        [SerializeField] private bool autoCollectChildWaypoints = true;

        public int Count => waypoints != null ? waypoints.Length : 0;
        public float PointReachRadius => pointReachRadius;

        private void Awake()
        {
            if (!autoCollectChildWaypoints || (waypoints != null && waypoints.Length > 0))
                return;

            int childCount = transform.childCount;
            if (childCount == 0)
                return;

            waypoints = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
                waypoints[i] = transform.GetChild(i);
        }

        public bool HasRoute => Count > 0;

        public Vector3 GetPosition(int index)
        {
            if (!HasRoute)
                return transform.position;

            index = Mathf.Clamp(index, 0, Count - 1);
            return waypoints[index] != null ? waypoints[index].position : transform.position;
        }

        public int GetNextIndex(int currentIndex)
        {
            if (!HasRoute)
                return 0;

            if (Count == 1)
                return 0;

            int next = currentIndex + 1;
            if (next >= Count)
                return loop ? 0 : Count - 1;

            return next;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!HasRoute)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < Count; i++)
            {
                if (waypoints[i] == null)
                    continue;

                Gizmos.DrawSphere(waypoints[i].position, pointReachRadius * 0.35f);

                int next = i + 1 < Count ? i + 1 : loop ? 0 : -1;
                if (next >= 0 && waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }
#endif
    }
}

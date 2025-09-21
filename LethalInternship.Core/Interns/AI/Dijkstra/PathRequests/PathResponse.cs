using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Dijkstra.PathRequests
{
    public struct PathResponse
    {
        public Vector3 start;
        public Vector3 target;

        public NavMeshPathStatus pathStatus;
        public Vector3[] path;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        public PathResponse(Vector3 start, Vector3 target, NavMeshPathStatus pathStatus, Vector3[] path, IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint)
        {
            this.start = start;
            this.target = target;
            this.pathStatus = pathStatus;
            this.path = path;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
        }
    }
}

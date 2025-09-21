using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Dijkstra.PathRequests
{
    public struct PathResponse
    {
        public NavMeshPathStatus pathStatus;
        public Vector3[] path;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        public PathResponse(NavMeshPathStatus pathStatus, Vector3[] path, IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint)
        {
            this.pathStatus = pathStatus;
            this.path = path;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
        }
    }
}

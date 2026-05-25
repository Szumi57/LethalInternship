using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public struct TimedCalculatePathResponse
    {
        public NavMeshPathStatus PathStatus;
        public NavMeshPath Path;
        public bool IsDirectlyReachable;

        public TimedCalculatePathResponse(NavMeshPathStatus pathStatus, NavMeshPath? path, bool isDirectlyReachable)
        {
            PathStatus = pathStatus;
            Path = path == null ? new NavMeshPath() : path;
            IsDirectlyReachable = isDirectlyReachable;
        }
    }

    public class TimedCalculatePath
    {
        private TimedCalculatePathResponse result = new TimedCalculatePathResponse();
        private NavMeshPath path = new NavMeshPath();

        private Vector3? previousDestination;
        private Vector3? currentDestination;

        private float timer = 1f;
        private float nextCheckTime;

        public TimedCalculatePathResponse GetPath(InternAI internAI, Vector3 destination, bool force = false)
        {
            if (NeedToRecalculate(destination) || force)
            {
                CalculatePath(internAI, destination);
                return result;
            }

            return result;
        }

        private bool NeedToRecalculate(Vector3 destination)
        {
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timer;
                return true;
            }

            previousDestination = currentDestination;
            currentDestination = destination;
            if (currentDestination != previousDestination)
            {
                return true;
            }

            return false;
        }

        private void CalculatePath(InternAI internAI, Vector3 destination)
        {
            Vector3 start = internAI.transform.position;
            if (Mathf.Abs(start.y - destination.y) > 100f)
            {
                result = new TimedCalculatePathResponse(NavMeshPathStatus.PathInvalid, path: null, isDirectlyReachable: false);
                return;
            }

            NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, path);
            result = new TimedCalculatePathResponse(path.status, path, isDirectlyReachable: true);
        }
    }
}

using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    internal class InstructionCalculatePathItems : IInstruction
    {
        public int IdBatch { get; private set; }
        public int GroupId { get; private set; }

        public Vector3 start;
        public Vector3 target;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        public float samplePosDist;

        private NavMeshPath navPath = new NavMeshPath();

        public InstructionCalculatePathItems(int idBatch, int groupId,
                                             Vector3 start, Vector3 target,
                                             IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint,
                                             float samplePosDist)
        {
            IdBatch = idBatch;
            GroupId = groupId;
            this.start = start;
            this.target = target;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
            this.samplePosDist = samplePosDist;
        }

        public void Execute()
        {
            NavMeshHit hitEnd;
            if (NavMesh.SamplePosition(target, out hitEnd, samplePosDist, NavMesh.AllAreas))
            {
                // Check if sampled position not too far
                float sqrHorizontalDistance = Vector3.Scale(hitEnd.position - target, new Vector3(1, 0, 1)).sqrMagnitude;
                // Close enough to item for grabbing
                if (sqrHorizontalDistance < 0.2f * 0.2f)
                {
                    target = hitEnd.position;
                }
                else
                {
                    PluginLoggerHook.LogDebug?.Invoke($"InstructionCalculatePathItems SamplePostoo far {sqrHorizontalDistance}, using target as before");
                }
            }

            NavMesh.CalculatePath(start, target, NavMesh.AllAreas, navPath);
            //PluginLoggerHook.LogDebug?.Invoke($"{(navPath.status == NavMeshPathStatus.PathComplete ? "+" : "")}Execute InstructionCalculatePathItems SamplePos({samplePosDist}), target {target}, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            if (navPath.status == NavMeshPathStatus.PathInvalid
                || navPath.status == NavMeshPathStatus.PathPartial) // no partials
            {
                return;
            }

            // Valid path
            float distance = Dijkstra.Dijkstra.GetFullDistancePath(navPath.corners);
            if (navPath.status == NavMeshPathStatus.PathComplete)
            {
                InternManager.Instance.CancelGroup(IdBatch, GroupId);
            }

            startDJKPoint.TryAddToNeighbors(targetDJKPoint.Id, target, distance);
            targetDJKPoint.TryAddToNeighbors(startDJKPoint.Id, start, distance);
        }
    }
}

using LethalInternship.Core.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public class InstructionCalculatePathNoPartialsSamplePos : InstructionBase
    {
        public override void Execute()
        {
            NavMeshHit hitEnd;
            if (NavMesh.SamplePosition(target, out hitEnd, samplePosDist, NavMesh.AllAreas))
            {
                // Check if sampled position not too far
                float sqrHorizontalDistance = Vector3.Scale(hitEnd.position - target, new Vector3(1, 0, 1)).sqrMagnitude;
                // Close enough to item for grabbing
                if (sqrHorizontalDistance < 1f * 1f)
                {
                    target = hitEnd.position;
                }
                else
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"InstructionCalculatePathItems SamplePostoo far {sqrHorizontalDistance}, using target as before");
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

            onNeighborResult(fromId, toId, start, target, distance);
        }

        public override void ReleaseInPool()
        {
            InternManager.Instance.Pools.Return(this);
        }
    }
}

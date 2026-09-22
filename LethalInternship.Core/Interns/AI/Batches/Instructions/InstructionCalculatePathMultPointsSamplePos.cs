using LethalInternship.Core.Managers;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public class InstructionCalculatePathMultPointsSamplePos : InstructionBase
    {
        public override void Execute()
        {
            NavMeshHit hitEnd;
            if (NavMesh.SamplePosition(target, out hitEnd, samplePosDist, NavMesh.AllAreas))
            {
                target = hitEnd.position;
            }

            NavMesh.CalculatePath(start, target, NavMesh.AllAreas, navPath);
            //PluginLoggerHook.LogDebug?.Invoke($"{(navPath.status == NavMeshPathStatus.PathComplete ? "+" : "")}Execute CalculatePathMultPoints SamplePos({samplePosDist}), target {target}, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            if (navPath.status == NavMeshPathStatus.PathInvalid)
            {
                return;
            }

            // Valid path
            float distance = Dijkstra.Dijkstra.GetFullDistancePath(navPath.corners);
            if (navPath.status == NavMeshPathStatus.PathPartial)
            {
                distance = Dijkstra.Dijkstra.ApplyPartialPathPenalty(distance, navPath.corners[^1], target);
            }
            onNeighborResult(fromId, toId, start, target, distance);

            if (navPath.status == NavMeshPathStatus.PathComplete)
            {
                InternManager.Instance.CancelGroup(IdBatch, GroupId);
            }
        }

        public override void ReleaseInPool()
        {
            Reset();
            InternManager.Instance.Pools.Return(this);
        }
    }
}

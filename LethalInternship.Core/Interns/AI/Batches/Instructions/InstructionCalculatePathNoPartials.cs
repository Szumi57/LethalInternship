using LethalInternship.Core.Managers;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public class InstructionCalculatePathNoPartials : InstructionBase
    {
        public override void Execute()
        {
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

            onNeighborResult(fromId,
                             toId,
                             start,
                             target,
                             distance);
        }

        public override void ReleaseInPool()
        {
            Reset();
            InternManager.Instance.Pools.Return(this);
        }
    }
}

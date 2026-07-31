using LethalInternship.Core.Managers;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public class InstructionCalculatePathWithSamplePos : InstructionBase
    {
        public override void Execute()
        {
            NavMeshHit hitEnd;
            if (NavMesh.SamplePosition(target, out hitEnd, samplePosDist, NavMesh.AllAreas))
            {
                target = hitEnd.position;
                NavMesh.CalculatePath(start, hitEnd.position, NavMesh.AllAreas, navPath);
                //PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePath SamplePos({samplePosDist}), target {target}, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            }
            else if (NavMesh.SamplePosition(target, out hitEnd, 10f, NavMesh.AllAreas))
            {
                target = hitEnd.position;
                NavMesh.CalculatePath(start, hitEnd.position, NavMesh.AllAreas, navPath);
                //PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePath SamplePos(10), target {target}, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            }
            else
            {
                NavMesh.CalculatePath(start, target, NavMesh.AllAreas, navPath);
                //PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePathWith SamplePos(failed), target {target}, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            }

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
            else if (navPath.status == NavMeshPathStatus.PathComplete)
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

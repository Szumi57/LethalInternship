using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public class InstructionCalculatePathWithSamplePos : IInstruction
    {
        public int IdBatch { get; private set; }
        public int GroupId { get; private set; }

        public Vector3 start;
        public Vector3 target;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        public float samplePosDist;

        private NavMeshPath navPath = new NavMeshPath();

        public InstructionCalculatePathWithSamplePos(int idBatch, int groupId,
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
                NavMesh.CalculatePath(start, hitEnd.position, NavMesh.AllAreas, navPath);
                PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePathWithSamplePos samplePosDist {samplePosDist}, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            }
            else if (NavMesh.SamplePosition(target, out hitEnd, 10f, NavMesh.AllAreas))
            {
                NavMesh.CalculatePath(start, hitEnd.position, NavMesh.AllAreas, navPath);
                PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePathWithSamplePos samplePosDist 10, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            }
            else
            {
                NavMesh.CalculatePath(start, target, NavMesh.AllAreas, navPath);
                PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePathWithSamplePos SamplePosition failed, {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            }

            float distance = Dijkstra.Dijkstra.GetFullDistancePath(navPath.corners);
            if (navPath.status == NavMeshPathStatus.PathPartial)
            {
                distance = Dijkstra.Dijkstra.ApplyPartialPathPenalty(distance, navPath.corners[^1], target);
            }

            startDJKPoint.TryAddToNeighbors(targetDJKPoint, distance);
            targetDJKPoint.TryAddToNeighbors(startDJKPoint, distance);

            InternManager.Instance.CancelGroup(IdBatch, GroupId);
        }
    }
}

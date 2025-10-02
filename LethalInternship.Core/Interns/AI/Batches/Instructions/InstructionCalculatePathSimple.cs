using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;
using UnityEngine.AI;

namespace LethalInternship.Core.Interns.AI.Batches.Instructions
{
    public class InstructionCalculatePathSimple : IInstruction
    {
        public int IdBatch { get; private set; }
        public int GroupId { get; private set; }

        public Vector3 start;
        public Vector3 target;

        public IDJKPoint startDJKPoint;
        public IDJKPoint targetDJKPoint;

        private NavMeshPath navPath = new NavMeshPath();

        public InstructionCalculatePathSimple(int idBatch, int groupId,
                                              Vector3 start, Vector3 target,
                                              IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint)
        {
            IdBatch = idBatch;
            GroupId = groupId;
            this.start = start;
            this.target = target;
            this.startDJKPoint = startDJKPoint;
            this.targetDJKPoint = targetDJKPoint;
        }

        public void Execute()
        {
            NavMesh.CalculatePath(start, target, NavMesh.AllAreas, navPath);
            PluginLoggerHook.LogDebug?.Invoke($"Execute InstructionCalculatePathSimple {startDJKPoint.Id}-{targetDJKPoint.Id} batch {IdBatch} groupid {GroupId}, status {navPath.status}");
            if (navPath.status == NavMeshPathStatus.PathInvalid)
            {
                return;
            }

            float distance = Dijkstra.Dijkstra.GetFullDistancePath(navPath.corners);
            if (navPath.status == NavMeshPathStatus.PathPartial)
            {
                distance = Dijkstra.Dijkstra.ApplyPartialPathPenalty(distance, navPath.corners[^1], target);
            }

            startDJKPoint.TryAddToNeighbors(targetDJKPoint.Id, target, distance);
            targetDJKPoint.TryAddToNeighbors(startDJKPoint.Id, start, distance);

            InternManager.Instance.CancelGroup(IdBatch, GroupId);
        }
    }
}

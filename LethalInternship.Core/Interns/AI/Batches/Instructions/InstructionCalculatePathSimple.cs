using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Managers;
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

            if (navPath.status == NavMeshPathStatus.PathComplete)
            {
                float distance = Dijkstra.Dijkstra.GetFullDistancePath(navPath.corners);
                distance = distance < 1 ? 1 : distance;
                startDJKPoint.TryAddToNeighbors(targetDJKPoint, distance);
                targetDJKPoint.TryAddToNeighbors(startDJKPoint, distance);

                InternManager.Instance.CancelGroup(IdBatch, GroupId);
            }
            else if (navPath.status == NavMeshPathStatus.PathPartial)
            {
                float distance = Dijkstra.Dijkstra.GetFullDistancePath(navPath.corners);
                distance += 100000000f;
                startDJKPoint.TryAddToNeighbors(targetDJKPoint, distance);
                targetDJKPoint.TryAddToNeighbors(startDJKPoint, distance);
                
                InternManager.Instance.CancelGroup(IdBatch, GroupId);
            }
        }
    }
}

using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public delegate void NeighborResult(int fromId,
                                        int toId,
                                        Vector3 start,
                                        Vector3 target,
                                        float distance);

    public interface IInstruction
    {
        int IdBatch { get; }
        int GroupId { get; }
        void Initialize(int idBatch, int groupId,
                        Vector3 start, Vector3 target,
                        IDJKPoint startDJKPoint, IDJKPoint targetDJKPoint,
                        float samplePosDist,
                        int fromId,
                        int toId,
                        NeighborResult resultCallback);
        void Execute();
        void ReleaseInPool();
    }
}

using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public struct DJKNeighbor
    {
        public int ToId;
        public Vector3 Pos;
        public float Cost;

        public DJKNeighbor(int toId, Vector3 pos, float cost)
        {
            ToId = toId;
            Pos = pos;
            Cost = cost;
        }
    }
}

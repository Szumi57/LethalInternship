using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public interface IDJKPoint
    {
        int Id { get; set; }

        List<(IDJKPoint neighbor, float weight)> Neighbors { get; }

        bool IsNeighborExist(IDJKPoint neighbor);
        float? GetNeighborDistanceIfExist(IDJKPoint neighbor);
        bool TryAddToNeighbors(IDJKPoint neighborToAdd, float weight);

        Vector3[] GetAllPoints();
        Vector3 GetClosestPointFrom(Vector3 point);
    }
}

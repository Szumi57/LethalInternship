using LethalInternship.Core.Interns.AI.Batches.Instructions;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.Parameters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints
{
    public class DJKEntrancePoint : IDJKPoint
    {
        public int Id { get; set; }
        public EntranceTeleport Entrance1 { get; set; }
        public EntranceTeleport? Entrance2 { get; set; }

        public List<(int idNeighbor, Vector3 neighborPos, float weight)> Neighbors { get; private set; }

        public DJKEntrancePoint(EntranceTeleport entrance)
        {
            Id = 0;
            Entrance1 = entrance;

            Neighbors = new List<(int idNeighbor, Vector3 neighborPos, float weight)>();
        }

        public object Clone()
        {
            var copy = new DJKEntrancePoint(Entrance1);
            copy.Entrance2 = Entrance2; 
            copy.Id = Id;
            copy.Neighbors = Neighbors
                .Select(n => (n.idNeighbor, n.neighborPos, n.weight))
                .ToList();
            return copy;
        }

        public bool TryAddOtherEntrance(EntranceTeleport entrance2)
        {
            //PluginLoggerHook.LogDebug?.Invoke($"id: {Id}, {entrance2.entrancePoint} =? {Entrance1.exitPoint}");
            if (entrance2 != Entrance1
                && entrance2.entranceId == Entrance1.entranceId)
            {
                //PluginLoggerHook.LogDebug?.Invoke($"new entrance2 !!!");
                Entrance2 = entrance2;
                return true;
            }

            return false;
        }

        public Vector3 GetClosestPointTo(Vector3 point)
        {
            if (Entrance2 == null)
            {
                return Entrance1.entrancePoint.position;
            }

            if ((point - Entrance1.entrancePoint.position).sqrMagnitude < (point - Entrance2.entrancePoint.position).sqrMagnitude)
            {
                return Entrance1.entrancePoint.position;
            }
            else
            {
                return Entrance2.entrancePoint.position;
            }
        }

        public Vector3 GetExitPointFrom(Vector3 point)
        {
            if (Entrance2 == null)
            {
                return Entrance1.entrancePoint.position;
            }

            if ((point - Entrance1.entrancePoint.position).sqrMagnitude < (point - Entrance2.entrancePoint.position).sqrMagnitude)
            {
                return Entrance2.entrancePoint.position;
            }
            else
            {
                return Entrance1.entrancePoint.position;
            }
        }

        public bool TryAddToNeighbors(int idNeighborToAdd, Vector3 neighborToAddPos, float weight)
        {
            if (!Neighbors.Any(x => x.idNeighbor == idNeighborToAdd))
            {
                Neighbors.Add((idNeighborToAdd, neighborToAddPos, weight));
                return true;
            }

            return false;
        }

        public bool IsNeighborExist(int idNeighbor)
        {
            return Neighbors.Any(x => x.idNeighbor == idNeighbor);
        }

        public Vector3 GetNeighborPos(int idNeighbor)
        {
            return Neighbors.First(x => x.idNeighbor == idNeighbor).neighborPos;
        }

        public void SetNeighborPos(int idNeighbor, Vector3 newPos)
        {
            var neighbor = Neighbors.First(x => x.idNeighbor == idNeighbor);
            neighbor.neighborPos = newPos;
        }

        public Vector3[] GetAllPoints()
        {
            List<Vector3> points = new List<Vector3>
            {
                Entrance1.entrancePoint.position
            };
            if (Entrance2 != null)
            {
                points.Add(Entrance2.entrancePoint.position);
            }

            return points.ToArray();
        }

        public Vector3[] GetNearbyPoints(Vector3 point)
        {
            List<Vector3> points = new List<Vector3>
            {
                Entrance1.entrancePoint.position
            };
            if (Entrance2 != null)
            {
                points.Add(Entrance2.entrancePoint.position);
            }

            return points
                        .Where(p => p.y - point.y <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
                        .OrderBy(p => (p - point).sqrMagnitude)
                        .ToArray();
        }

        public IInstruction GenerateInstruction(int idBatch, InstructionParameters instructionToProcess)
        {
            return new InstructionCalculatePathSimple(
                                idBatch,
                                instructionToProcess.groupId,
                                start: instructionToProcess.start,
                                target: instructionToProcess.target,
                                startDJKPoint: instructionToProcess.startDJKPoint,
                                targetDJKPoint: instructionToProcess.targetDJKPoint);
        }

        public override string ToString()
        {
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.idNeighbor}({(int)Mathf.Sqrt(x.weight)})"));
            string entrance2 = "null";
            if (Entrance2 != null)
            {
                entrance2 = $"{{{Entrance2.entrancePoint.position}}}";
            }

            return $"DJKEntrancePoint id:{Id}, Entrance1:{{{Entrance1.entrancePoint.position}}}, Entrance2: {entrance2}, Neighbors {{{neighborsString}}}";
        }
    }
}

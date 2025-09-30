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

        public List<(IDJKPoint neighbor, float weight)> Neighbors { get; }

        private List<(Vector3 previousNeighborPoint, Vector3 thisPoint)> reachablePoints;

        public DJKEntrancePoint(EntranceTeleport entrance)
        {
            Id = 0;
            Entrance1 = entrance;

            Neighbors = new List<(IDJKPoint neighbor, float weight)>();
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

        public bool TryAddToNeighbors(IDJKPoint neighborToAdd, float weight)
        {
            if (!Neighbors.Any(x => x.neighbor.Id == neighborToAdd.Id))
            {
                Neighbors.Add((neighborToAdd, weight));
                return true;
            }

            return false;
        }

        public bool IsNeighborExist(IDJKPoint neighbor)
        {
            return Neighbors.Any(x => x.neighbor.Id == neighbor.Id);
        }

        public float? GetNeighborDistanceIfExist(IDJKPoint neighbor)
        {
            if (IsNeighborExist(neighbor))
            {
                return Neighbors.FirstOrDefault(x => x.neighbor.Id == neighbor.Id).weight;
            }
            return null;
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
                        .Where(p => (p - point).sqrMagnitude <= Const.OUTSIDE_INSIDE_DISTANCE_LIMIT)
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
            string neighborsString = string.Join(",", Neighbors.Select(x => $"{x.neighbor.Id}({(int)Mathf.Sqrt(x.weight)})"));
            string entrance2 = "null";
            if (Entrance2 != null)
            {
                entrance2 = $"{{{Entrance2.entrancePoint.position}}}";
            }

            return $"DJKEntrancePoint id:{Id}, Entrance1:{{{Entrance1.entrancePoint.position}}}, Entrance2: {entrance2}, Neighbors {{{neighborsString}}}";
        }
    }
}

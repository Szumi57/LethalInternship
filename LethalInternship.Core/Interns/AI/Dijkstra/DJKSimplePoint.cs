using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class DJKSimplePoint : IDJKPoint
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public List<(IDJKPoint neighbor, float weight)> Neighbors { get; }

        public DJKSimplePoint(int id, Vector3 position)
        {
            Id = id;
            Position = position;
            Neighbors = new List<(IDJKPoint neighbor, float weight)>();
        }

        public DJKSimplePoint(int id, Vector3 position, string name)
        {
            Name = name;
            Id = id;
            Position = position;
            Neighbors = new List<(IDJKPoint neighbor, float weight)>();
        }

        public bool IsNeighborExist(IDJKPoint neighbor)
        {
            return Neighbors.Any(x => x.neighbor.Id == neighbor.Id);
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

        public Vector3[] GetAllPoints()
        {
            return new Vector3[] { Position };
        }

        public override string ToString()
        {
            string neighborsString = string.Join(",", Neighbors.Select(x => x.neighbor.Id));

            return $"DJKSimplePoint \"{Name}\" id:{Id}, Position: {Position}, Neighbors {{{neighborsString}}}";
        }
    }
}

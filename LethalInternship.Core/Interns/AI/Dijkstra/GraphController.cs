using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Linq;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class GraphController
    {
        public List<IDJKPoint> DJKPoints { get; set; }

        public GraphController()
        {
            DJKPoints = new List<IDJKPoint>();
        }

        public GraphController(List<IDJKPoint> graph)
        {
            DJKPoints = new List<IDJKPoint>(graph);
        }

        public GraphController(GraphController graph)
        {
            DJKPoints = new List<IDJKPoint>(graph.DJKPoints);
        }

        public void AddPoint(IDJKPoint point)
        {
            point.Id = DJKPoints.Count == 0 ? 0 : DJKPoints.Max(x => x.Id) + 1;
            DJKPoints.Add(point);
        }

        public void CleanNeighbors()
        {
            List<int> neighborsPresent = DJKPoints.Select(x => x.Id).ToList();
            foreach (var point in DJKPoints)
            {
                point.Neighbors.RemoveAll(n => !neighborsPresent.Contains(n.neighbor.Id));
            }
        }

        public override string ToString()
        {
            string pathString = $"Graph({(DJKPoints == null ? 0 : DJKPoints.Count)})=";
            if (DJKPoints == null)
            {
                return string.Concat(pathString, " null");
            }
            else if (DJKPoints.Count == 0)
            {
                return string.Concat(pathString, " empty");
            }
            else
            {
                return string.Concat(pathString, string.Join("\r\n                                                               ", DJKPoints));
            }
        }
    }
}

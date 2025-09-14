using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.Dijkstra
{
    public class CalculateNeighborsParameters
    {
        public List<IDJKPoint> DJKPointsGraph { get; }
        public bool CalculateFinished { get; }

        public CalculateNeighborsParameters(List<IDJKPoint> dJKPointsGraph)
        {
            DJKPointsGraph = dJKPointsGraph;
            CalculateFinished = false;
        }
    }
}

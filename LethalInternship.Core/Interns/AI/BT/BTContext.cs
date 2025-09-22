using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.Core.Interns.AI.Dijkstra;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT
{
    public class BTContext
    {
        public InternAI InternAI { get; set; }

        public PathController PathController { get; set; }

        public SearchCoroutineController searchForPlayers { get; set; }

        public CoroutineController PanikCoroutine { get; set; }
        public CoroutineController LookingAroundCoroutineController { get; set; }
        public CoroutineController searchingWanderCoroutineController { get; set; }
        public CoroutineController CalculatePathCoroutineController { get; set; }
    }
}

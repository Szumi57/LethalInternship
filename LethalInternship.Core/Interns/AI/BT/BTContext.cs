using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.Core.Interns.AI.Dijkstra;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT
{
    public class BTContext
    {
        public InternAI InternAI { get; set; } = null!;

        public PathController PathController { get; set; } = null!;
        public DJKPointMapper DJKPointMapper { get; set; } = null!;

        public SearchCoroutineController searchForPlayers { get; set; } = null!;

        public EnemyAI? CurrentEnemy;

        // Items
        public GrabbableObject? TargetItem;
        public int nbItemsToCheck;
        public bool cancelScavenging;

        // No use for now, target always known
        public Vector3? TargetLastKnownPosition;

        public CoroutineController PanikCoroutine { get; set; } = null!;
        public CoroutineController LookingAroundCoroutineController { get; set; } = null!;
        public CoroutineController searchingWanderCoroutineController { get; set; } = null!;
        public CoroutineController CalculatePathCoroutineController { get; set; } = null!;
    }
}

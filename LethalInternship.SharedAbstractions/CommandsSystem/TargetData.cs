using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public struct TargetData
    {
        public GameObject Root;
        public float Distance { get { return RaycastHit.distance; } }
        public RaycastHit RaycastHit;

        public IInternAI? Intern;
        public GrabbableObject? Item;
        public EnemyAI? Enemy;

        public IPointOfInterest? PointedPointOfInterest;

        public bool IsTargetNotPosition()
        {
            return Intern != null
                || Enemy != null
                || Item != null;
        }

        public override string ToString()
        {
            string target = string.Empty;
            if (Intern != null)
            {
                return $"Intern ({Intern.Npc.playerUsername}) dist {Distance.ToString("00.00")}, Root {Root}";
            }
            if (Enemy != null)
            {
                return $"Enemy ({Enemy.enemyType.enemyName}) dist {Distance.ToString("00.00")}, Root {Root}";
            }
            if (Item != null)
            {
                return $"Item({Item.itemProperties.itemName}) dist {Distance.ToString("00.00")}, Root {Root}";
            }
            if (PointedPointOfInterest != null)
            {
                return $"Point of interest ({PointedPointOfInterest.GetPoint().ToString()}) dist {Distance.ToString("00.00")}, Root {Root}";
            }
            return $"Target RaycastHit ({RaycastHit.ToString()}) dist {Distance.ToString("00.00")}, Root {Root}";
        }
    }
}

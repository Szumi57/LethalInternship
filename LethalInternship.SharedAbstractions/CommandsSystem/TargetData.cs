using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public struct TargetData
    {
        public GameObject Root;
        public float Distance;

        public IInternAI? Intern;
        public GrabbableObject? Item;
        public EnemyAI? Enemy;

        public IPointOfInterest? PointOfInterest;

        public bool IsTargetNotEmpty()
        {
            return Intern != null
                || Enemy != null
                || Item != null
                || PointOfInterest != null;
        }

        public bool IsTargetNotPointOfInterest()
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
                return $"Intern ({Intern.Npc.playerUsername}) dist {Distance}";
            }
            if (Enemy != null)
            {
                return $"Enemy ({Enemy.enemyType.enemyName}) dist {Distance}";
            }
            if (Item != null)
            {
                return $"Item({Item.itemProperties.itemName}) dist {Distance}";
            }
            if (PointOfInterest != null)
            {
                return $"Point of interest ({PointOfInterest.GetPoint().ToString()}) dist {Distance}";
            }
            return target;
        }
    }
}

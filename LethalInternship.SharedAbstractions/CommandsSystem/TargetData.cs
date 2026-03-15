using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.CommandsSystem
{
    public struct TargetData
    {
        public GameObject Root;

        public IInternAI? Intern;
        public GrabbableObject? Item;
        public EnemyAI? Enemy;

        public IPointOfInterest? PointOfInterest;

        public float Score;

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
    }
}

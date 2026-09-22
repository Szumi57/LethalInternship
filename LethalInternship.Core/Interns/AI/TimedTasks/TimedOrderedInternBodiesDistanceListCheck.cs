using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedOrderedInternBodiesDistanceListCheck
    {
        private List<IInternCullingBodyInfo> orderedInternBodiesDistanceList = null!;

        private float timer = 0.2f;
        private float nextCheckTime;

        public List<IInternCullingBodyInfo> GetOrderedInternDistanceList(List<IInternCullingBodyInfo> internBodies)
        {
            if (orderedInternBodiesDistanceList == null)
            {
                orderedInternBodiesDistanceList = new List<IInternCullingBodyInfo>();
            }

            if (!NeedToRecalculate())
            {
                return orderedInternBodiesDistanceList;
            }

            CalculateOrderedInternDistanceList(internBodies);
            return orderedInternBodiesDistanceList;
        }

        private bool NeedToRecalculate()
        {
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timer;
                return true;
            }
            return false;
        }

        private void CalculateOrderedInternDistanceList(List<IInternCullingBodyInfo> internBodies)
        {
            orderedInternBodiesDistanceList.Clear();
            orderedInternBodiesDistanceList.AddRange(internBodies);
            orderedInternBodiesDistanceList.Sort((a, b) => a.GetSqrDistanceWithLocalPlayer()
                                                           .CompareTo(b.GetSqrDistanceWithLocalPlayer()));
        }
    }
}

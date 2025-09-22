using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedOrderedInternBodiesDistanceListCheck
    {
        private List<IInternCullingBodyInfo> orderedInternBodiesDistanceList = null!;

        private long timer = 200 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

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
            long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;
            if (elapsedTime > timer)
            {
                lastTimeCalculate = DateTime.Now.Ticks;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CalculateOrderedInternDistanceList(List<IInternCullingBodyInfo> internBodies)
        {
            orderedInternBodiesDistanceList.Clear();
            orderedInternBodiesDistanceList.AddRange(internBodies);
            orderedInternBodiesDistanceList = orderedInternBodiesDistanceList
                                                .OrderBy(x => x.GetSqrDistanceWithLocalPlayer())
                                                .ToList();
        }
    }
}

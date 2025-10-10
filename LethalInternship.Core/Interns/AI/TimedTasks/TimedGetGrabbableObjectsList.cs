using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGrabbableObjectsList
    {
        private List<GameObject> grabbableObjectsInMap = new List<GameObject>();

        private long timer = 10000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        public List<GameObject> GetGrabbableObjectsList()
        {
            if (!NeedToRecalculate())
            {
                grabbableObjectsInMap.TrimExcess();
                return grabbableObjectsInMap;
            }

            GetList();
            return grabbableObjectsInMap;
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

        private void GetList()
        {
            grabbableObjectsInMap.Clear();

            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < array.Length; i++)
            {
                grabbableObjectsInMap.Add(array[i].gameObject);
            }
        }
    }
}

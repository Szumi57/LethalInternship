using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetGrabbableObjectsList
    {
        private List<GameObject> grabbableObjectsInMap = new List<GameObject>();

        private float timer = 10f;
        private float nextCheckTime;

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
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timer;
                return true;
            }
            return false;
        }

        private void GetList()
        {
            grabbableObjectsInMap.Clear();

            GrabbableObject[] array = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
            for (int i = 0; i < array.Length; i++)
            {
                grabbableObjectsInMap.Add(array[i].gameObject);
            }
        }
    }
}

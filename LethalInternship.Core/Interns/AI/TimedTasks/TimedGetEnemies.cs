using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetEnemies
    {
        private List<EnemyAI> enemiesInMap = new List<EnemyAI>();

        private int lastNbSpawnEnemies;
        private float timer = 7f;
        private float nextCheckTime;

        public List<EnemyAI> GetEnemiesList()
        {
            if (!NeedToRecalculate())
            {
                enemiesInMap.TrimExcess();
                return enemiesInMap;
            }

            GetList();
            return enemiesInMap;
        }

        private bool NeedToRecalculate()
        {
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timer;
                return true;
            }

            if (lastNbSpawnEnemies != RoundManager.Instance.SpawnedEnemies.Count)
            {
                lastNbSpawnEnemies = RoundManager.Instance.SpawnedEnemies.Count;
                return true;
            }

            return false;
        }

        private void GetList()
        {
            var timer = new Stopwatch();
            timer.Start();

            enemiesInMap.Clear();
            enemiesInMap = UnityEngine.Object.FindObjectsByType<EnemyAI>(UnityEngine.FindObjectsSortMode.None)
                .Where(x => !(x is IInternAI))
                .ToList();

            timer.Stop();
        }
    }
}

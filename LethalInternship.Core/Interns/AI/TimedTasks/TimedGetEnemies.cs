using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetEnemies
    {
        private List<EnemyAI> enemiesInMap = new List<EnemyAI>();

        private int lastNbSpawnEnemies;
        private long timer = 7000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

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
            long elapsedTime = DateTime.Now.Ticks - lastTimeCalculate;

            if (lastNbSpawnEnemies != RoundManager.Instance.SpawnedEnemies.Count)
            {
                lastNbSpawnEnemies = RoundManager.Instance.SpawnedEnemies.Count;
                return true;
            }

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
            var timer = new Stopwatch();
            timer.Start();

            enemiesInMap.Clear();
            enemiesInMap = UnityEngine.Object.FindObjectsByType<EnemyAI>(UnityEngine.FindObjectsSortMode.None).ToList();

            timer.Stop();
        }
    }
}

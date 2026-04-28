using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class CheckForEnemies : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            int range = Const.INTERN_ENTITIES_RANGE;

            if (context.ClosestEnemies == null)
                context.ClosestEnemies = new HashSet<EnemyAI>();
            else
                context.ClosestEnemies.Clear();

            // Fog reduce the visibility
            if (ai.isOutside && !ai.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
                range = Mathf.Clamp(range, 0, 30);

            foreach (EnemyAI spawnedEnemy in InternManager.Instance.GetEnemiesList())
            {
                if (spawnedEnemy.GetType() == typeof(InternAI))
                    continue;

                if (spawnedEnemy.isEnemyDead
                    || ai.isOutside != spawnedEnemy.isOutside)
                    continue;

                // Enemy close enough ?
                float sqrDistanceToEnemy = (spawnedEnemy.transform.position - ai.Npc.gameplayCamera.transform.position).sqrMagnitude;
                if (sqrDistanceToEnemy > range * range)
                    continue;

                context.ClosestEnemies.Add(spawnedEnemy);
                //Debug.Log($"new ClosestEnemies {spawnedEnemy.enemyType.enemyName}");
            }

            return BehaviourTreeStatus.Success;
        }
    }
}

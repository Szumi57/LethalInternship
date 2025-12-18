using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class EnemySeen : IBTCondition
    {
        private const int SAME_ENEMY_COUNTER_MAX = 10;
        private int sameEnemyCounter;

        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;

            if (context.CurrentEnemy != null
                && context.CurrentEnemy.isEnemyDead)
            {
                context.CurrentEnemy = null;
            }
            if (context.CurrentEnemy != null)
            {
                sameEnemyCounter++;
                if (sameEnemyCounter >= SAME_ENEMY_COUNTER_MAX)
                {
                    context.CurrentEnemy = null;
                }
                else
                {
                    // Skip checking for enemies
                    return true;
                }
            }

            if (ai.NpcController.IsControllerInCruiser)
            {
                return false;
            }

            // Check for enemies
            EnemyAI? enemyAI = CheckLOSForEnemy(ai,
                                                Const.INTERN_FOV,
                                                Const.INTERN_ENTITIES_RANGE,
                                                ai.isOutside ? Const.PROXIMITY_AWARENESS_OUTSIDE : Const.PROXIMITY_AWARENESS_INSIDE);
            if (enemyAI == null)
            {
                return false;
            }

            PluginLoggerHook.LogDebug?.Invoke($"EnemySeen {enemyAI}");
            context.CurrentEnemy = enemyAI;
            sameEnemyCounter = 0;
            return true;
        }

        /// <summary>
        /// Check if enemy in line of sight.
        /// </summary>
        /// <param name="width">FOV of the intern</param>
        /// <param name="range">Distance max for seeing something</param>
        /// <param name="proximityAwareness">Distance where the interns "sense" the player, in line of sight or not. -1 for no proximity awareness</param>
        /// <returns>Enemy <c>EnemyAI</c> or null</returns>
        private EnemyAI? CheckLOSForEnemy(InternAI ai,
                                          float width = 45f, int range = 20, int proximityAwareness = -1)
        {
            // Fog reduce the visibility
            if (ai.isOutside && !ai.enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = Mathf.Clamp(range, 0, 30);
            }

            StartOfRound instanceSOR = StartOfRound.Instance;
            Transform thisInternCamera = ai.Npc.gameplayCamera.transform;
            foreach (EnemyAI spawnedEnemy in InternManager.Instance.GetEnemiesList())
            {
                if (spawnedEnemy.GetType() == typeof(InternAI))
                {
                    continue;
                }

                if (spawnedEnemy.isEnemyDead)
                {
                    continue;
                }

                if (ai.isOutside != spawnedEnemy.isOutside)
                {
                    continue;
                }

                // Enemy close enough ?
                Vector3 positionEnemy = spawnedEnemy.transform.position;
                Vector3 directionEnemyFromCamera = positionEnemy - thisInternCamera.position;
                float sqrDistanceToEnemy = directionEnemyFromCamera.sqrMagnitude;
                if (sqrDistanceToEnemy > range * range)
                {
                    continue;
                }

                // Fear range
                float? fearRange = ai.GetFearRangeForEnemies(spawnedEnemy);
                if (!fearRange.HasValue)
                {
                    continue;
                }

                if (sqrDistanceToEnemy > fearRange * fearRange)
                {
                    continue;
                }
                // Enemy in distance of fear range

                // Proximity awareness, danger
                if (proximityAwareness > -1
                    && sqrDistanceToEnemy < (float)(proximityAwareness * proximityAwareness))
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} DANGER CLOSE \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return spawnedEnemy;
                }

                if (Physics.Linecast(thisInternCamera.position, positionEnemy, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    // Obstructed
                    continue;
                }

                // Line of Sight, danger
                if (Vector3.Angle(thisInternCamera.forward, directionEnemyFromCamera) < width)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} DANGER LOS \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    return spawnedEnemy;
                }
            }

            return null;
        }
    }
}

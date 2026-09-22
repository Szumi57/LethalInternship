using LethalInternship.SharedAbstractions.Constants;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ConditionNodes
{
    public class IsInDanger : IBTCondition
    {
        public bool Condition(BTContext context)
        {
            InternAI ai = context.InternAI;
            Transform thisInternCamera = ai.Npc.gameplayCamera.transform;
            StartOfRound instanceSOR = StartOfRound.Instance;

            float fov = Const.INTERN_FOV;
            int proximityAwareness = ai.isOutside ? Const.PROXIMITY_AWARENESS_OUTSIDE : Const.PROXIMITY_AWARENESS_INSIDE;

            EnemyAI? tooCloseEnemy = null;
            float sqrDistTooCloseEnemy = float.MaxValue;

            foreach (EnemyAI enemy in context.ClosestEnemies)
            {
                if (enemy == null
                    || enemy.isEnemyDead
                    || ai.isOutside != enemy.isOutside)
                {
                    continue;
                }

                Vector3 positionEnemy = enemy.transform.position;
                Vector3 directionEnemyFromCamera = positionEnemy - thisInternCamera.position;
                float sqrDistanceToEnemy = directionEnemyFromCamera.sqrMagnitude;

                // Fear range
                float? fearRange = ai.GetFearRangeForEnemies(enemy);
                if (!fearRange.HasValue)
                {
                    continue;
                }

                if (sqrDistanceToEnemy > fearRange * fearRange)
                {
                    continue;
                }
                // Enemy in distance of fear range

                if (sqrDistanceToEnemy < 1.5f * 1.5f)
                {
                    // Panic range
                    if (sqrDistanceToEnemy < sqrDistTooCloseEnemy)
                    {
                        sqrDistTooCloseEnemy = sqrDistanceToEnemy;
                        tooCloseEnemy = enemy;
                        continue;
                    }
                }

                if (Physics.Linecast(thisInternCamera.position, positionEnemy, instanceSOR.collidersAndRoomMaskAndDefault))
                {
                    // Obstructed
                    continue;
                }

                // Proximity awareness (only when not obstructed), danger
                if (proximityAwareness > -1
                    && sqrDistanceToEnemy < (float)(proximityAwareness * proximityAwareness))
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} DANGER CLOSE \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    if (sqrDistanceToEnemy < sqrDistTooCloseEnemy)
                    {
                        sqrDistTooCloseEnemy = sqrDistanceToEnemy;
                        tooCloseEnemy = enemy;
                        continue;
                    }
                }

                // Line of Sight, danger
                if (Vector3.Angle(thisInternCamera.forward, directionEnemyFromCamera) < fov)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"{ai.Npc.playerUsername} DANGER LOS \"{spawnedEnemy.enemyType.enemyName}\" {spawnedEnemy.enemyType.name}");
                    if (sqrDistanceToEnemy < sqrDistTooCloseEnemy)
                    {
                        sqrDistTooCloseEnemy = sqrDistanceToEnemy;
                        tooCloseEnemy = enemy;
                        continue;
                    }
                }
            }

            context.CurrentEnemy = tooCloseEnemy;
            return context.CurrentEnemy != null;
        }
    }
}

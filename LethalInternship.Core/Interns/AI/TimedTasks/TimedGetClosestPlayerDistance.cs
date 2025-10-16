using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using System;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedGetClosestPlayerDistance
    {
        private float distance;

        private long timer = 1000 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        public float GetClosestPlayerDistance(Vector3 internPos)
        {
            if (!NeedToRecalculate())
            {
                return distance;
            }

            CalculateGetClosestPlayerDistance(internPos);
            return distance;
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

        private void CalculateGetClosestPlayerDistance(Vector3 internPos)
        {
            float minDistance = float.MaxValue;

            // Distance with real players
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                if (!player.isPlayerControlled
                    || player.isPlayerDead)
                {
                    continue;
                }

                float currDist = (player.transform.position - internPos).sqrMagnitude;
                if (currDist < minDistance)
                {
                    minDistance = currDist;
                }
            }

            // Distance with ship
            if (InternManager.Instance.ShipTransform == null)
            {
                distance = minDistance;
                return;
            }

            float distWithShip = (InternManager.Instance.ShipTransform.position - internPos).sqrMagnitude * 2;
            if (distWithShip < minDistance)
            {
                minDistance = distWithShip;
            }

            distance = minDistance;
        }
    }
}

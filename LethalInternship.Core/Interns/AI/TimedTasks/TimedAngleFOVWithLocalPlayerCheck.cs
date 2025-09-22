using System;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedAngleFOVWithLocalPlayerCheck
    {
        private float angle;

        private long timer = 50 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        public float GetAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
        {
            if (!NeedToRecalculate())
            {
                return angle;
            }

            CalculateAngleFOVWithLocalPlayer(localPlayerCameraTransform, internBodyPos);
            return angle;
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

        private void CalculateAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
        {
            angle = Vector3.Angle(localPlayerCameraTransform.forward, internBodyPos - localPlayerCameraTransform.position);
        }
    }
}

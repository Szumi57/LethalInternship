using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedAngleFOVWithLocalPlayerCheck
    {
        private float angle;

        private float timer = 0.05f;
        private float nextCheckTime;

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
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + timer;
                return true;
            }
            return false;
        }

        private void CalculateAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
        {
            angle = Vector3.Angle(localPlayerCameraTransform.forward, internBodyPos - localPlayerCameraTransform.position);
        }
    }
}

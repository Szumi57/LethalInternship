using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedTouchingGroundCheck
    {
        private bool isTouchingGround = true;
        private RaycastHit groundHit;
        private string groundHitColliderName = string.Empty;

        private float timer = 0.2f;
        private float nextCheckTime;

        public bool IsTouchingGround(Vector3 internPosition, bool forceCalculation = false)
        {
            if (!NeedToRecalculate()
                && !forceCalculation)
            {
                return isTouchingGround;
            }

            CalculateTouchingGround(internPosition);
            return isTouchingGround;
        }

        public RaycastHit GetGroundHit(Vector3 internPosition)
        {
            if (!NeedToRecalculate())
            {
                return groundHit;
            }

            CalculateTouchingGround(internPosition);
            return groundHit;
        }

        public string GetGroundHitColliderName(Vector3 internPosition)
        {
            if (!NeedToRecalculate())
            {
                return groundHitColliderName;
            }

            CalculateTouchingGround(internPosition);
            return groundHitColliderName;
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

        private void CalculateTouchingGround(Vector3 internPosition)
        {
            if (Physics.Raycast(new Ray(internPosition + Vector3.up, -Vector3.up),
                                               out groundHit,
                                               2.5f,
                                               StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                isTouchingGround = true;
                groundHitColliderName = groundHit.collider.name;
            }
            else
            {
                isTouchingGround = false;
                groundHitColliderName = string.Empty;
            }
        }
    }
}

using System;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.TimedTasks
{
    public class TimedTouchingGroundCheck
    {
        private bool isTouchingGround = true;
        private RaycastHit groundHit;

        private long timer = 200 * TimeSpan.TicksPerMillisecond;
        private long lastTimeCalculate;

        public bool IsTouchingGround(Vector3 internPosition)
        {
            if (!NeedToRecalculate())
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

        private void CalculateTouchingGround(Vector3 internPosition)
        {
            isTouchingGround = Physics.Raycast(new Ray(internPosition + Vector3.up, -Vector3.up),
                                               out groundHit,
                                               2.5f,
                                               StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
        }
    }
}

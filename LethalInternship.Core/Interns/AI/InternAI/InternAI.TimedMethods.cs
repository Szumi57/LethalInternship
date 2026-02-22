using LethalInternship.Core.Interns.AI.TimedTasks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public TimedTouchingGroundCheck IsTouchingGroundTimedCheck = new TimedTouchingGroundCheck();
        public TimedAngleFOVWithLocalPlayerCheck AngleFOVWithLocalPlayerTimedCheck = new TimedAngleFOVWithLocalPlayerCheck();
        private TimedGetClosestPlayerDistance GetClosestPlayerDistanceTimed = new TimedGetClosestPlayerDistance();

        public float GetAngleFOVWithLocalPlayer(Transform localPlayerCameraTransform, Vector3 internBodyPos)
        {
            return this.AngleFOVWithLocalPlayerTimedCheck.GetAngleFOVWithLocalPlayer(localPlayerCameraTransform, internBodyPos);
        }

        public float GetClosestPlayerDistance()
        {
            if (this.NpcController == null
                || this.Npc == null)
            {
                return float.MaxValue;
            }

            if (this.IsEnemyDead
                || this.Npc.isPlayerDead)
            {
                return float.MaxValue;
            }

            return GetClosestPlayerDistanceTimed.GetClosestPlayerDistance(this.Npc.transform.position);
        }
    }
}

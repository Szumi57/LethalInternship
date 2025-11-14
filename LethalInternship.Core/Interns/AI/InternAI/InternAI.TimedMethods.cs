using LethalInternship.Core.Interns.AI.TimedTasks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public TimedTouchingGroundCheck IsTouchingGroundTimedCheck = null!;
        public TimedAngleFOVWithLocalPlayerCheck AngleFOVWithLocalPlayerTimedCheck = null!;
        private TimedGetClosestPlayerDistance GetClosestPlayerDistanceTimed = null!;

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

            if (GetClosestPlayerDistanceTimed == null)
            {
                GetClosestPlayerDistanceTimed = new TimedGetClosestPlayerDistance();
            }

            return GetClosestPlayerDistanceTimed.GetClosestPlayerDistance(this.Npc.transform.position);
        }
    }
}

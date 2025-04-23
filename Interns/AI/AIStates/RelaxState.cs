using GameNetcodeStuff;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Interns.AI.Commands;

namespace LethalInternship.Interns.AI.AIStates
{
    public class RelaxState : AIState
    {
        private PlayerControllerB targetPlayer { get { return ai.targetPlayer; } }

        public RelaxState(InternAI ai) : base(ai)
        {
            CurrentState = EnumAIStates.Relax;
        }

        public RelaxState(AIState state) : base(state)
        {
            CurrentState = EnumAIStates.Relax;
        }

        public override void DoAI()
        {
            // Check for enemies
            EnemyAI? enemyAI = ai.CheckLOSForEnemy(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (enemyAI != null)
            {
                ai.State = new PanikState(this, enemyAI);
                return;
            }

            // Check for object to grab
            if (ai.AreHandsFree())
            {
                GrabbableObject? grabbableObject = ai.LookingForObjectToGrab();
                if (grabbableObject != null)
                {
                    ai.QueueNewPriorityCommand(new FetchingObjectCommand(ai, grabbableObject));
                }
            }
            else
            {
                // Or drop in ship room
                if (npcController.Npc.isInHangarShipRoom)
                {
                    // Intern drop item
                    ai.DropItem();
                }
            }

            // Is target player in vehicle ?
            VehicleController? vehicleController = ai.GetVehicleCruiserTargetPlayerIsIn();
            if (vehicleController != null)
            {
                ai.QueueNewCommand(new GoToCruiserCommand(ai, vehicleController));
            }

            // Execute command
            //ai.ExecuteEndCommand(ai.GetNewCommand().Execute());

            // Copy movement
            FollowCrouchStateIfCan();
        }

        private void FollowCrouchStateIfCan()
        {
            if (npcController.Npc.isCrouching)
            {
                npcController.OrderToToggleCrouch();
                return;
            }

            if (Plugin.Config.FollowCrouchWithPlayer
                && targetPlayer != null)
            {
                if (targetPlayer.isCrouching
                    && !npcController.Npc.isCrouching)
                {
                    npcController.OrderToToggleCrouch();
                }
                else if (!targetPlayer.isCrouching
                        && npcController.Npc.isCrouching)
                {
                    npcController.OrderToToggleCrouch();
                }
            }
        }

        public override string GetBillboardStateIndicator()
        {
            return ai.CurrentCommand == null ? string.Empty: ai.CurrentCommand.GetBillboardStateIndicator();
        }

        public override void TryPlayCurrentStateVoiceAudio()
        {
            //ai.InternIdentity.Voice.StopAudioFadeOut();
        }
    }
}

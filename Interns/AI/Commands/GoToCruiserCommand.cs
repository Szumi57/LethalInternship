using LethalInternship.Constants;
using LethalInternship.Enums;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalInternship.Interns.AI.Commands
{
    public class GoToCruiserCommand : ICommandAI
    {
        private readonly InternAI ai;
        private readonly VehicleController? vehicleController;
        private NpcController Controller { get { return ai.NpcController; } }

        public GoToCruiserCommand(InternAI ai, VehicleController vehicleController)
        {
            this.ai = ai;
            this.vehicleController = vehicleController;
        }

        public EnumCommandEnd Execute()
        {
            ai.SetAgent(enabled: false);

            if (vehicleController == null)
            {
                ai.QueueNewCommand(new FollowPlayerCommand(ai));
                return EnumCommandEnd.Finished;
            }

            Vector3 entryPointInternCruiser = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomEntryPosCruiser();

            if (Controller.IsControllerInCruiser)
            {
                if (ai.GetVehicleCruiserTargetPlayerIsIn() == null)
                {
                    // Exit vehicle cruiser
                    ai.SyncTeleportInternVehicle(entryPointInternCruiser, enteringVehicle: false, vehicleController);
                    vehicleController.SetVehicleCollisionForPlayer(true, Controller.Npc);

                    Plugin.LogDebug("gotocruiser add follow");
                    ai.QueueNewCommand(new FollowPlayerCommand(ai));
                    return EnumCommandEnd.Finished;
                }

                // Try play voice
                TryPlayCurrentStateVoiceAudio();

                // Stay in vehicle with target player
                return EnumCommandEnd.Repeat;
            }

            // Intern still not in vehicle

            // No target player, search for one
            // Or Target is not available anymore
            if (ai.targetPlayer == null
                || !ai.PlayerIsTargetable(ai.targetPlayer))
            {
                ai.QueueNewCommand(new LookingForPlayer(ai));
                return EnumCommandEnd.Finished;
            }

            // Teleport to cruiser and enter vehicle
            // Place intern in random spot
            Vector3 internPassengerPos = vehicleController.transform.position + vehicleController.transform.rotation * GetNextRandomInCruiserPos();
            ai.SyncTeleportInternVehicle(internPassengerPos, enteringVehicle: true, vehicleController);

            // random rotation
            float angleRandom = Random.Range(-180f, 180f);
            Controller.UpdateNowTurnBodyTowardsDirection(Quaternion.Euler(0, angleRandom, 0) * Controller.Npc.thisController.transform.forward);

            // Crouch or not
            float crouchRancom = Random.Range(0f, 1f);
            if (crouchRancom > 0.5f
                && !Controller.Npc.isCrouching)
            {
                Controller.OrderToToggleCrouch();
            }

            // Stop animations
            Controller.StopAnimations();

            // Chill
            Controller.OrderToStopMoving();

            return EnumCommandEnd.Repeat;
        }

        public string GetBillboardStateIndicator()
        {
            return string.Empty;
        }

        public void PlayerHeard(Vector3 noisePosition) { }

        private void TryPlayCurrentStateVoiceAudio()
        {
            // Default states, wait for cooldown and if no one is talking close
            ai.InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.EnteringCruiser,
                CanTalkIfOtherInternTalk = false,
                WaitForCooldown = true,
                CutCurrentVoiceStateToTalk = false,
                CanRepeatVoiceState = true,

                ShouldSync = true,
                IsInternInside = Controller.Npc.isInsideFactory,
                AllowSwearing = Plugin.Config.AllowSwearing.Value
            });
        }

        private Vector3 GetNextRandomInCruiserPos()
        {
            float x = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.x, Const.SECOND_CORNER_INTERN_IN_CRUISER.x);
            float y = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.y, Const.SECOND_CORNER_INTERN_IN_CRUISER.y);
            float z = Random.Range(Const.FIRST_CORNER_INTERN_IN_CRUISER.z, Const.SECOND_CORNER_INTERN_IN_CRUISER.z);

            return new Vector3(x, y, z);
        }

        private Vector3 GetNextRandomEntryPosCruiser()
        {
            float x = Random.Range(Const.POS1_ENTRY_INTERN_CRUISER.x, Const.POS2_ENTRY_INTERN_CRUISER.x);

            return new Vector3(x, Const.POS1_ENTRY_INTERN_CRUISER.y, Const.POS1_ENTRY_INTERN_CRUISER.z);
        }
    }
}

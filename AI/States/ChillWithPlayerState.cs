using GameNetcodeStuff;
using JetBrains.Annotations;
using LethalInternship.Enums;
using LethalInternship.Patches;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.AI.States
{
    internal class ChillWithPlayerState : State
    {
        private static readonly EnumStates STATE = EnumStates.ChillWithPlayer;
        public override EnumStates GetState() { return STATE; }

        private float SqrHorizontalDistanceWithTarget
        {
            get
            {
                //return (ai.targetPlayer.transform.position - ai.transform.position).sqrMagnitude;
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        private float SqrVerticalDistanceWithTarget
        {
            get
            {
                //return (ai.targetPlayer.transform.position - ai.transform.position).sqrMagnitude;
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(0, 1, 0)).sqrMagnitude;
            }
        }

        public ChillWithPlayerState(State state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
        }

        public override void DoAI()
        {
            // Check for object to grab
            if (PlayerControllerBPatch.FirstEmptyItemSlot_ReversePatch(npcController.Npc) > -1)
            {
                GameObject gameObjectGrabbleObject = ai.CheckLineOfSightForObjects();
                if (gameObjectGrabbleObject)
                {
                    GrabbableObject component = gameObjectGrabbleObject.GetComponent<GrabbableObject>();
                    if (component && !component.isHeld)
                    {
                        ai.SetDestinationToPositionInternAI(gameObjectGrabbleObject.transform.position);
                        this.targetItem = component;
                        ai.State = new FetchingObjectState(this);
                        return;
                    }
                }
            }

            if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                || SqrVerticalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                // todo check sound
                npcController.OrderToLookForward();

                PlayerControllerB? player = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
                if (player != null && ai.PlayerIsTargetable(player))
                {
                    // Target still here but too far
                    targetLastKnownPosition = player.transform.position;
                    ai.State = new GetCloseToPlayerState(this);
                    return;
                }
                else
                {
                    // Target lost
                    ai.State = new JustLostPlayerState(this);
                    return;
                }
            }

            PlayerControllerB? target = ai.CheckLOSForTarget(Const.INTERN_FOV, 50, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (target == null)
            {
                // Target is not visible
                ai.State = new GetCloseToPlayerState(this);
                return;
            }
            else
            {
                // Target still visible
                targetLastKnownPosition = target.transform.position;
            }

            // Looking
            PlayerControllerB? playerToLook = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (playerToLook != null)
            {
                npcController.OrderToLookAtPlayer(playerToLook);
            }
            else
            {
                npcController.OrderToLookForward();
            }

            // Chill
            ai.StopMoving();
        }
    }
}

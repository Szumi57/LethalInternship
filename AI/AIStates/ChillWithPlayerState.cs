using GameNetcodeStuff;
using LethalInternship.Enums;
using UnityEngine;

namespace LethalInternship.AI.AIStates
{
    internal class ChillWithPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.ChillWithPlayer;
        public override EnumAIStates GetAIState() { return STATE; }

        private float SqrHorizontalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(1, 0, 1)).sqrMagnitude;
            }
        }

        private float SqrVerticalDistanceWithTarget
        {
            get
            {
                return Vector3.Scale((ai.targetPlayer.transform.position - npcController.Npc.transform.position), new Vector3(0, 1, 0)).sqrMagnitude;
            }
        }

        public ChillWithPlayerState(AIState state) : base(state)
        {
            if (searchForPlayers.inProgress)
            {
                ai.StopSearch(searchForPlayers, true);
            }
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
                    ai.State = new FetchingObjectState(this, grabbableObject);
                    return;
                }
            }

            // Target in ship, wait outside
            if (ai.IsTargetInShipBoundsExpanded())
            {
                ai.State = new PlayerInShipState(this);
                return;
            }

            // Update target last known position
            PlayerControllerB? player = ai.CheckLOSForTarget(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                targetLastKnownPosition = ai.targetPlayer.transform.position;
            }

            // Target too far
            if (SqrHorizontalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_HOR * Const.DISTANCE_CLOSE_ENOUGH_HOR
                || SqrVerticalDistanceWithTarget > Const.DISTANCE_CLOSE_ENOUGH_VER * Const.DISTANCE_CLOSE_ENOUGH_VER)
            {
                // todo check sound
                npcController.OrderToLookForward();
                ai.State = new GetCloseToPlayerState(this);
                return;
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

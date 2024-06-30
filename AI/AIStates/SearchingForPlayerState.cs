using GameNetcodeStuff;
using LethalInternship.Enums;

namespace LethalInternship.AI.AIStates
{
    internal class SearchingForPlayerState : AIState
    {
        private static readonly EnumAIStates STATE = EnumAIStates.SearchingForPlayer;
        public override EnumAIStates GetAIState() { return STATE; }

        private PlayerControllerB? player;

        public SearchingForPlayerState(AIState newState) : base(newState) { }
        public SearchingForPlayerState(InternAI ai) : base(ai) { }

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

            player = ai.CheckLOSForClosestPlayer(Const.INTERN_FOV, Const.INTERN_ENTITIES_RANGE, (int)Const.DISTANCE_CLOSE_ENOUGH_HOR);
            if (player != null)
            {
                // new target
                ai.SyncAssignTargetAndSetMovingTo(player);
                return;
            }

            ai.OrderMoveToDestination();

            if (!searchForPlayers.inProgress)
            {
                ai.StartSearch(ai.transform.position, searchForPlayers);
            }
        }
    }
}

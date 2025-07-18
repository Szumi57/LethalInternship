using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.BT.ActionNodes;
using LethalInternship.Core.Interns.AI.BT.ConditionNodes;
using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.BT
{
    public class BTController
    {
        public IBehaviourTreeNode BehaviorTree = null!;

        // Routine controllers
        private CoroutineController panikCoroutineController = null!;
        private CoroutineController lookingAroundCoroutineController = null!;
        private CoroutineController searchingWanderCoroutineController = null!;
        private SearchCoroutineController searchForPlayers = null!;

        // Action nodes
        Dictionary<string, IBTAction> actions = null!;
        // Condition nodes
        Dictionary<string, IBTCondition> conditions = null!;

        // Data context
        private BTContext BTContext = null!;

        public BTController(InternAI internAI)
        {
            InitCoroutineControllers(internAI);
            InitNodes();
            InitContext(internAI);

            BehaviorTree = CreateTree();

            //BTUtil.PrintTree(CreateTree());
            //PluginLoggerHook.LogDebug?.Invoke($"{BTUtil.Export1TreeJson(BehaviorTree)}");
        }

        public void TickTree(float deltaTime)
        {
            panikCoroutineController.Reset();
            lookingAroundCoroutineController.Reset();
            searchingWanderCoroutineController.Reset();
            searchForPlayers.Reset();

            BehaviorTree.Tick(new TimeData(deltaTime));

            panikCoroutineController.CheckCoroutine();
            lookingAroundCoroutineController.CheckCoroutine();
            searchingWanderCoroutineController.CheckCoroutine();
            searchForPlayers.CheckCoroutine();
        }

        private void InitCoroutineControllers(InternAI internAI)
        {
            panikCoroutineController = new CoroutineController(internAI);
            lookingAroundCoroutineController = new CoroutineController(internAI);
            searchingWanderCoroutineController = new CoroutineController(internAI);
            searchForPlayers = new SearchCoroutineController(internAI);
        }

        private void InitNodes()
        {
            // Action nodes
            actions = new Dictionary<string, IBTAction>()
            {
                { "CheckLOSForClosestPlayer", new CheckLOSForClosestPlayer() },
                { "Chill", new Chill() },
                { "ChillFrontOfEntrance", new ChillFrontOfEntrance() },
                { "DropItem", new DropItem() },
                { "EnterVehicle", new EnterVehicle() },
                { "ExitVehicle", new ExitVehicle() },
                { "FleeFromEnemy", new FleeFromEnemy() },
                { "GetClosestEntrance", new GetClosestEntrance() },
                { "GoToEntrance", new GoToEntrance() },
                { "GoToPosition", new GoToPosition() },
                { "GoToVehicle", new GoToVehicle() },
                { "GrabItemBehavior", new GrabItemBehavior() },
                { "InVehicle", new InVehicle() },
                { "LookingAround", new LookingAround() },
                { "LookingForPlayer", new LookingForPlayer() },
                { "SetNextPosTargetItem", new SetNextPosTargetItem() },
                { "SetNextPosTargetLastKnownPosition", new SetNextPosTargetLastKnownPosition() },
                { "TakeEntrance", new TakeEntrance() },
                { "TeleportDebugToPos", new TeleportDebugToPos() },
                { "UpdateLastKnownPos", new UpdateLastKnownPos() }
            };

            // Condition nodes
            conditions = new Dictionary<string, IBTCondition>()
            {
                { "EnemySeen", new EnemySeen() },
                { "ExitNotBlocked", new ExitNotBlocked() },
                { "HasItemAndInShip", new HasItemAndInShip() },
                { "IsCommandFollowPlayer", new IsCommandThis(EnumCommandTypes.FollowPlayer) },
                { "IsCommandGoToVehicle", new IsCommandThis(EnumCommandTypes.GoToVehicle) },
                { "IsCommandGoToPosition", new IsCommandThis(EnumCommandTypes.GoToPosition) },
                { "IsCommandWait", new IsCommandThis(EnumCommandTypes.Wait) },
                { "IsInternInVehicle", new IsInternInVehicle() },
                { "IsLastKnownPositionValid", new IsLastKnownPositionValid() },
                { "IsObjectToGrab", new IsObjectToGrab() },
                { "IsTargetInVehicle", new IsTargetInVehicle() },
                { "NextPositionIsAfterEntrance", new NextPositionIsAfterEntrance() },
                { "NotValidEntrance", new NotValidEntrance() },
                { "TargetValid", new TargetValid() },
                { "TooFarFromEntrance", new TooFarFromEntrance() },
                { "TooFarFromObject", new TooFarFromObject() },
                { "TooFarFromPos", new TooFarFromPos() },
                { "TooFarFromVehicle", new TooFarFromVehicle() }
            };
            
        }

        private void InitContext(InternAI internAI)
        {
            BTContext = new BTContext()
            {
                InternAI = internAI,
                LookingAroundCoroutineController = this.lookingAroundCoroutineController,
                PanikCoroutine = this.panikCoroutineController,
                searchForPlayers = this.searchForPlayers,
                searchingWanderCoroutineController = this.searchingWanderCoroutineController
            };
        }

        private IBehaviourTreeNode CreateTree()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Start")
                    .Sequence("Panik")
                        .Condition("<EnemySeen>", t => conditions["EnemySeen"].Condition(BTContext))
                        .Do("FleeFromEnemy", t => actions["FleeFromEnemy"].Action(BTContext))
                    .End()

                    .Sequence("Command go to position")
                        .Condition("<isCommand GoToPosition>", t => conditions["IsCommandGoToPosition"].Condition(BTContext))
                        .Selector("Go to position")
                            .Splice(CreateSubTreeGoToPosition())
                            .Do("Chill", t => actions["Chill"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Command go to vehicle")
                        .Condition("<isCommand GoToVehicle>", t => conditions["IsCommandGoToVehicle"].Condition(BTContext))
                        .Splice(CreateSubTreeGoToVehicle())
                    .End()

                    .Sequence("Fetch object")
                        .Condition("<IsObjectToGrab>", t => conditions["IsObjectToGrab"].Condition(BTContext))
                        .Do("SetNextPosTargetItem", t => actions["SetNextPosTargetItem"].Action(BTContext))
                        .Selector("Should go to position")
                            .Splice(CreateSubTreeGoToObject())
                            .Do("GrabObject", t => actions["GrabItemBehavior"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Command follow player")
                        .Condition("<isCommand FollowPlayer>", t => conditions["IsCommandFollowPlayer"].Condition(BTContext))
                        .Condition("<TargetValid>", t => conditions["TargetValid"].Condition(BTContext))
                        .Splice(CreateSubTreeFollowPlayer())
                    .End()

                    .Do("checkLOSForClosestPlayer", t => actions["CheckLOSForClosestPlayer"].Action(BTContext))
                    .Do("LookingForPlayer", t => actions["LookingForPlayer"].Action(BTContext))

                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToPosition()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Go to position")
                    .Sequence("Take entrance maybe")
                        .Condition("<nextPositionIsAfterEntrance>", t => conditions["NextPositionIsAfterEntrance"].Condition(BTContext))
                        .Selector("Take entrance")
                            .Sequence("Get entrance")
                                .Do("getClosestEntrance", t => actions["GetClosestEntrance"].Action(BTContext))
                                .Condition("<notValidEntrance>", t => conditions["NotValidEntrance"].Condition(BTContext))
                                .Do("teleportDebugToPos", t => actions["TeleportDebugToPos"].Action(BTContext))
                            .End()

                            .Sequence("Go to entrance")
                                .Condition("<tooFarFromEntrance>", t => conditions["TooFarFromEntrance"].Condition(BTContext))
                                .Do("Go to entrance", t => actions["GoToEntrance"].Action(BTContext))
                            .End()

                            .Sequence("Exit not blocked, take entrance")
                                .Condition("<exitNotBlocked>", t => conditions["ExitNotBlocked"].Condition(BTContext))
                                .Do("takeEntrance", t => actions["TakeEntrance"].Action(BTContext))
                            .End()

                            .Do("chillFrontOfEntrance", t => actions["ChillFrontOfEntrance"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Go to position if too far")
                        .Condition("tooFarFromPos", t => conditions["TooFarFromPos"].Condition(BTContext))
                        .Do("goToPosition", t => actions["GoToPosition"].Action(BTContext))
                    .End()

                    .Sequence("Drop item if in ship")
                        .Condition("hasItemAndInShip", t => conditions["HasItemAndInShip"].Condition(BTContext))
                        .Do("dropItem", t => actions["DropItem"].Action(BTContext))
                    .End()
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToObject()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Go to position")
                    .Sequence("Take entrance maybe")
                        .Condition("<nextPositionIsAfterEntrance>", t => conditions["NextPositionIsAfterEntrance"].Condition(BTContext))
                        .Selector("Take entrance")
                            .Sequence("Get entrance")
                                .Do("getClosestEntrance", t => actions["GetClosestEntrance"].Action(BTContext))
                                .Condition("<notValidEntrance>", t => conditions["NotValidEntrance"].Condition(BTContext))
                                .Do("teleportDebugToPos", t => actions["TeleportDebugToPos"].Action(BTContext))
                            .End()

                            .Sequence("Go to entrance")
                                .Condition("tooFarFromEntrance", t => conditions["TooFarFromEntrance"].Condition(BTContext))
                                .Do("Go to entrance", t => actions["GoToEntrance"].Action(BTContext))
                            .End()

                            .Sequence("Exit not blocked, take entrance")
                                .Condition("exitNotBlocked", t => conditions["ExitNotBlocked"].Condition(BTContext))
                                .Do("takeEntrance", t => actions["TakeEntrance"].Action(BTContext))
                            .End()

                            .Do("chillFrontOfEntrance", t => actions["ChillFrontOfEntrance"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Go to position")
                        .Condition("tooFarFromObject", t => conditions["TooFarFromObject"].Condition(BTContext))
                        .Do("goToPosition", t => actions["GoToPosition"].Action(BTContext))
                    .End()
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToVehicle()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Go to vehicle")
                    .Sequence("In vehicle")
                        .Condition("<isInternInVehicle>", t => conditions["IsInternInVehicle"].Condition(BTContext))
                        .Do("inVehicle", t => actions["InVehicle"].Action(BTContext))
                    .End()

                    .Sequence("Take entrance maybe")
                        .Condition("<nextPositionIsAfterEntrance>", t => conditions["NextPositionIsAfterEntrance"].Condition(BTContext))
                        .Selector("Take entrance")
                            .Sequence("Get entrance")
                                .Do("getClosestEntrance", t => actions["GetClosestEntrance"].Action(BTContext))
                                .Condition("<notValidEntrance>", t => conditions["NotValidEntrance"].Condition(BTContext))
                                .Do("enterVehicle", t => actions["EnterVehicle"].Action(BTContext))
                            .End()

                            .Sequence("Go to entrance")
                                .Condition("<tooFarFromEntrance>", t => conditions["TooFarFromEntrance"].Condition(BTContext))
                                .Do("Go to entrance", t => actions["GoToEntrance"].Action(BTContext))
                            .End()

                            .Sequence("Exit not blocked, take entrance")
                                .Condition("<exitNotBlocked>", t => conditions["ExitNotBlocked"].Condition(BTContext))
                                .Do("takeEntrance", t => actions["TakeEntrance"].Action(BTContext))
                            .End()

                            .Do("chillFrontOfEntrance", t => actions["ChillFrontOfEntrance"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Go to vehicle")
                        .Condition("<tooFarFromVehicle>", t => conditions["TooFarFromVehicle"].Condition(BTContext))
                        .Do("goToVehicle", t => actions["GoToVehicle"].Action(BTContext))
                    .End()

                    .Do("enterVehicle", t => actions["EnterVehicle"].Action(BTContext))
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeFollowPlayer()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Should follow player")
                    .Sequence("Target in vehicle")
                        .Condition("<isTargetInVehicle>", t => conditions["IsTargetInVehicle"].Condition(BTContext))
                        .Splice(CreateSubTreeGoToVehicle())
                    .End()

                    .Sequence("Intern in vehicle, exit")
                        .Condition("<isInternInVehicle>", t => conditions["IsInternInVehicle"].Condition(BTContext))
                        .Do("exitVehicle", t => actions["ExitVehicle"].Action(BTContext))
                    .End()

                    .Sequence("Follow player")
                        .Do("updateLastKnownPos", t => actions["UpdateLastKnownPos"].Action(BTContext))
                        .Condition("<isLastKnownPositionValid>", t => conditions["IsLastKnownPositionValid"].Condition(BTContext))
                        .Do("SetNextPosTargetLastKnownPosition", t => actions["SetNextPosTargetLastKnownPosition"].Action(BTContext))
                        .Selector("Go to pos or chill")
                            .Splice(CreateSubTreeGoToPosition())
                            .Do("chill", t => actions["Chill"].Action(BTContext))
                        .End()
                    .End()

                    .Do("LookingAround", t => actions["LookingAround"].Action(BTContext))
                .End()
                .Build();
        }
    }
}

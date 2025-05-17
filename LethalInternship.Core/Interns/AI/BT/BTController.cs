using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.BT.ActionNodes;
using LethalInternship.Core.Interns.AI.BT.ConditionNodes;
using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;

namespace LethalInternship.Core.Interns.AI.BT
{
    public class BTController
    {
        public IBehaviourTreeNode BehaviorTree = null!;

        private InternAI internAI = null!;

        // Routine controllers
        public CoroutineController panikCoroutineController = null!;
        public CoroutineController lookingAroundCoroutineController = null!;
        public CoroutineController searchingWanderCoroutineController = null!;
        public SearchCoroutineController searchForPlayers = null!;

        // Action nodes
        private CheckLOSForClosestPlayer checkLOSForClosestPlayer = null!;
        private Chill chill = null!;
        private ChillFrontOfEntrance chillFrontOfEntrance = null!;
        private DropItem dropItem = null!;
        private EnterVehicle enterVehicle = null!;
        private ExitVehicle exitVehicle = null!;
        private FleeFromEnemy fleeFromEnemy = null!;
        private GetClosestEntrance getClosestEntrance = null!;
        private GoToEntrance goToEntrance = null!;
        private GoToPosition goToPosition = null!;
        private GoToVehicle goToVehicle = null!;
        private GrabItemBehavior grabItemBehavior = null!;
        private InVehicle inVehicle = null!;
        private LookingAround lookingAround = null!;
        private LookingForPlayer lookingForPlayer = null!;
        private SetNextPos setNextPos = null!;
        private TakeEntrance takeEntrance = null!;
        private TeleportDebugToPos teleportDebugToPos = null!;
        private UpdateLastKnownPos updateLastKnownPos = null!;

        // Condition nodes
        private EnemySeen enemySeen = null!;
        private ExitNotBlocked exitNotBlocked = null!;
        private HasItemAndInShip hasItemAndInShip = null!;
        private IsCommandThis isCommandThis = null!;
        private IsInternInVehicle isInternInVehicle = null!;
        private IsLastKnownPositionValid isLastKnownPositionValid = null!;
        private IsObjectToGrab isObjectToGrab = null!;
        private IsTargetInVehicle isTargetInVehicle = null!;
        private NextPositionIsAfterEntrance nextPositionIsAfterEntrance = null!;
        private NotValidEntrance notValidEntrance = null!;
        private TargetValid targetValid = null!;
        private TooFarFromEntrance tooFarFromEntrance = null!;
        private TooFarFromObject tooFarFromObject = null!;
        private TooFarFromPos tooFarFromPos = null!;
        private TooFarFromVehicle tooFarFromVehicle = null!;

        public BTController(InternAI internAI)
        {
            this.internAI = internAI;
        }

        public void Init()
        {
            InitCoroutineControllers();
            InitNodes();
            BehaviorTree = CreateTree();

            BTUtil.PrintTree(BehaviorTree);
            PluginLoggerHook.LogDebug?.Invoke($"{BTUtil.Export1TreeJson(BehaviorTree)}");
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

        private void InitCoroutineControllers()
        {
            panikCoroutineController = new CoroutineController(internAI);
            lookingAroundCoroutineController = new CoroutineController(internAI);
            searchingWanderCoroutineController = new CoroutineController(internAI);
            searchForPlayers = new SearchCoroutineController(internAI);
        }

        private void InitNodes()
        {
            // Action nodes
            checkLOSForClosestPlayer = new CheckLOSForClosestPlayer();
            chill = new Chill();
            chillFrontOfEntrance = new ChillFrontOfEntrance();
            dropItem = new DropItem();
            enterVehicle = new EnterVehicle();
            exitVehicle = new ExitVehicle();
            fleeFromEnemy = new FleeFromEnemy();
            getClosestEntrance = new GetClosestEntrance();
            goToEntrance = new GoToEntrance();
            goToPosition = new GoToPosition();
            goToVehicle = new GoToVehicle();
            grabItemBehavior = new GrabItemBehavior();
            inVehicle = new InVehicle();
            lookingAround = new LookingAround();
            lookingForPlayer = new LookingForPlayer();
            setNextPos = new SetNextPos();
            takeEntrance = new TakeEntrance();
            teleportDebugToPos = new TeleportDebugToPos();
            updateLastKnownPos = new UpdateLastKnownPos();

            // Condition nodes
            enemySeen = new EnemySeen();
            exitNotBlocked = new ExitNotBlocked();
            hasItemAndInShip = new HasItemAndInShip();
            isCommandThis = new IsCommandThis();
            isInternInVehicle = new IsInternInVehicle();
            isLastKnownPositionValid = new IsLastKnownPositionValid();
            isObjectToGrab = new IsObjectToGrab();
            isTargetInVehicle = new IsTargetInVehicle();
            nextPositionIsAfterEntrance = new NextPositionIsAfterEntrance();
            notValidEntrance = new NotValidEntrance();
            targetValid = new TargetValid();
            tooFarFromEntrance = new TooFarFromEntrance();
            tooFarFromObject = new TooFarFromObject();
            tooFarFromPos = new TooFarFromPos();
            tooFarFromVehicle = new TooFarFromVehicle();
        }

        private IBehaviourTreeNode CreateTree()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Start")
                    .Sequence("Panik")
                        .Condition("<EnemySeen>", t => enemySeen.Condition(internAI))
                        .Do("fleeFromEnemy", t => fleeFromEnemy.Action(internAI, panikCoroutineController))
                    .End()

                    .Sequence("Command go to position")
                        .Condition("<isCommand GoToPosition>", t => isCommandThis.Condition(internAI, EnumCommandTypes.GoToPosition))
                        .Selector("Go to position")
                            .Splice(CreateSubTreeGoToPosition())
                            .Do("chill", t => chill.Action(internAI))
                        .End()
                    .End()

                    .Sequence("Command go to vehicle")
                        .Condition("<isCommand GoToVehicle>", t => isCommandThis.Condition(internAI, EnumCommandTypes.GoToVehicle))
                        .Splice(CreateSubTreeGoToVehicle())
                    .End()

                    .Sequence("Fetch object")
                        .Condition("<IsObjectToGrab>", t => isObjectToGrab.Condition(internAI))
                        .Do("setNextPos", t => setNextPos.Action(internAI, internAI.TargetItem.transform.position))
                        .Selector("Should go to position")
                            .Splice(CreateSubTreeGoToObject())
                            .Do("GrabObject", t => grabItemBehavior.Action(internAI))
                        .End()
                    .End()

                    .Sequence("Command follow player")
                        .Condition("<isCommand FollowPlayer>", t => isCommandThis.Condition(internAI, EnumCommandTypes.FollowPlayer))
                        .Condition("<TargetValid>", t => targetValid.Condition(internAI))
                        .Splice(CreateSubTreeFollowPlayer())
                    .End()

                    .Do("checkLOSForClosestPlayer", t => checkLOSForClosestPlayer.Action(internAI))
                    .Do("LookingForPlayer", t => lookingForPlayer.Action(internAI, searchingWanderCoroutineController, searchForPlayers))

                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToPosition()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Go to position")
                    .Sequence("Take entrance maybe")
                        .Condition("<nextPositionIsAfterEntrance>", t => nextPositionIsAfterEntrance.Condition(internAI, internAI.NextPos))
                        .Selector("Take entrance")
                            .Sequence("Get entrance")
                                .Do("getClosestEntrance", t => getClosestEntrance.Action(internAI, internAI.EntrancesTeleportArray))
                                .Condition("<notValidEntrance>", t => notValidEntrance.Condition(internAI))
                                .Do("teleportDebugToPos", t => teleportDebugToPos.Action(internAI, internAI.NextPos))
                            .End()

                            .Sequence("Go to entrance")
                                .Condition("<tooFarFromEntrance>", t => tooFarFromEntrance.Condition(internAI))
                                .Do("Go to entrance", t => goToEntrance.Action(internAI))
                            .End()

                            .Sequence("Exit not blocked, take entrance")
                                .Condition("<exitNotBlocked>", t => exitNotBlocked.Condition(internAI))
                                .Do("takeEntrance", t => takeEntrance.Action(internAI))
                            .End()

                            .Do("chillFrontOfEntrance", t => chillFrontOfEntrance.Action(internAI))
                        .End()
                    .End()

                    .Sequence("Go to position if too far")
                        .Condition("tooFarFromPos", t => tooFarFromPos.Condition(internAI, internAI.NextPos))
                        .Do("goToPosition", t => goToPosition.Action(internAI, internAI.NextPos))
                    .End()

                    .Sequence("Drop item if in ship")
                        .Condition("hasItemAndInShip", t => hasItemAndInShip.Condition(internAI))
                        .Do("dropItem", t => dropItem.Action(internAI))
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
                        .Condition("<nextPositionIsAfterEntrance>", t => nextPositionIsAfterEntrance.Condition(internAI, internAI.NextPos))
                        .Selector("Take entrance")
                            .Sequence("Get entrance")
                                .Do("getClosestEntrance", t => getClosestEntrance.Action(internAI, internAI.EntrancesTeleportArray))
                                .Condition("<notValidEntrance>", t => notValidEntrance.Condition(internAI))
                                .Do("teleportDebugToPos", t => teleportDebugToPos.Action(internAI, internAI.NextPos))
                            .End()

                            .Sequence("Go to entrance")
                                .Condition("tooFarFromEntrance", t => tooFarFromEntrance.Condition(internAI))
                                .Do("Go to entrance", t => goToEntrance.Action(internAI))
                            .End()

                            .Sequence("Exit not blocked, take entrance")
                                .Condition("exitNotBlocked", t => exitNotBlocked.Condition(internAI))
                                .Do("takeEntrance", t => takeEntrance.Action(internAI))
                            .End()

                            .Do("chillFrontOfEntrance", t => chillFrontOfEntrance.Action(internAI))
                        .End()
                    .End()

                    .Sequence("Go to position")
                        .Condition("tooFarFromObject", t => tooFarFromObject.Condition(internAI, internAI.TargetItem))
                        .Do("goToPosition", t => goToPosition.Action(internAI, internAI.NextPos))
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
                        .Condition("<isInternInVehicle>", t => isInternInVehicle.Condition(internAI))
                        .Do("inVehicle", t => inVehicle.Action(internAI))
                    .End()

                    .Sequence("Take entrance maybe")
                        .Condition("<nextPositionIsAfterEntrance>", t => nextPositionIsAfterEntrance.Condition(internAI, internAI.NextPos))
                        .Selector("Take entrance")
                            .Sequence("Get entrance")
                                .Do("getClosestEntrance", t => getClosestEntrance.Action(internAI, internAI.EntrancesTeleportArray))
                                .Condition("<notValidEntrance>", t => notValidEntrance.Condition(internAI))
                                .Do("enterVehicle", t => enterVehicle.Action(internAI))
                            .End()

                            .Sequence("Go to entrance")
                                .Condition("<tooFarFromEntrance>", t => tooFarFromEntrance.Condition(internAI))
                                .Do("Go to entrance", t => goToEntrance.Action(internAI))
                            .End()

                            .Sequence("Exit not blocked, take entrance")
                                .Condition("<exitNotBlocked>", t => exitNotBlocked.Condition(internAI))
                                .Do("takeEntrance", t => takeEntrance.Action(internAI))
                            .End()

                            .Do("chillFrontOfEntrance", t => chillFrontOfEntrance.Action(internAI))
                        .End()
                    .End()

                    .Sequence("Go to vehicle")
                        .Condition("<tooFarFromVehicle>", t => tooFarFromVehicle.Condition(internAI))
                        .Do("goToVehicle", t => goToVehicle.Action(internAI))
                    .End()

                    .Do("enterVehicle", t => enterVehicle.Action(internAI))
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeFollowPlayer()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Should follow player")
                    .Sequence("Target in vehicle")
                        .Condition("<isTargetInVehicle>", t => isTargetInVehicle.Condition(internAI))
                        .Splice(CreateSubTreeGoToVehicle())
                    .End()

                    .Sequence("Intern in vehicle, exit")
                        .Condition("<isInternInVehicle>", t => isInternInVehicle.Condition(internAI))
                        .Do("exitVehicle", t => exitVehicle.Action(internAI))
                    .End()

                    .Sequence("Follow player")
                        .Do("updateLastKnownPos", t => updateLastKnownPos.Action(internAI))
                        .Condition("<isLastKnownPositionValid>", t => isLastKnownPositionValid.Condition(internAI))
                        .Do("setNextPos", t => setNextPos.Action(internAI, internAI.TargetLastKnownPosition.Value))
                        .Selector("Go to pos or chill")
                            .Splice(CreateSubTreeGoToPosition())
                            .Do("chill", t => chill.Action(internAI))
                        .End()
                    .End()

                    .Do("LookingAround", t => lookingAround.Action(internAI, lookingAroundCoroutineController))
                .End()
                .Build();
        }
    }
}

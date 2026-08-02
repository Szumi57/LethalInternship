using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.BT.ActionNodes;
using LethalInternship.Core.Interns.AI.BT.ConditionNodes;
using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;

namespace LethalInternship.Core.Interns.AI.BT
{
    public class BTController
    {
        public IBehaviourTreeNode BehaviorTree = null!;

        // Routine controllers
        private SearchCoroutineController searchForPlayers = null!;
        private List<CoroutineController> CoroutineControllers = null!;

        // Action nodes
        Dictionary<string, IBTAction> actions = null!;
        // Condition nodes
        Dictionary<string, IBTCondition> conditions = null!;

        // Data context
        private BTContext BTContext = null!;

        private DJKMovingPoint _movingPlayerPoint = new DJKMovingPoint();
        private DJKMovingPoint _movingEnemyPoint = new DJKMovingPoint();
        private DJKItemPoint _itemPoint = new DJKItemPoint();
        private DJKStaticPoint _staticInterestPoint = new DJKStaticPoint();
        private DJKVehiclePoint _vehiclePoint = new DJKVehiclePoint("Cruiser interest point");
        private IDJKPoint? _tempPoint;

        public BTController(InternAI internAI)
        {
            InitCoroutineControllers(internAI);
            InitNodes();
            InitContext(internAI);

            BehaviorTree = CreateTree();

            BTUtil.PrintTree(CreateTree());
            PluginLoggerHook.LogDebug?.Invoke($"{BTUtil.Export1TreeJson(BehaviorTree)}");
        }

        public void TickTree(float deltaTime)
        {
            searchForPlayers.Reset();
            foreach (var controller in CoroutineControllers)
            {
                controller.Reset();
            }

            BehaviorTree.Tick(new TimeData(deltaTime));

            searchForPlayers.CheckCoroutine();
            foreach (var controller in CoroutineControllers)
            {
                controller.CheckCoroutine();
            }
        }

        private void InitCoroutineControllers(InternAI internAI)
        {
            CoroutineControllers = new List<CoroutineController>();
            for (int i = 0; i < 4; i++)
            {
                CoroutineControllers.Add(new CoroutineController(internAI));
            }

            searchForPlayers = new SearchCoroutineController(internAI);
        }

        private void InitNodes()
        {
            // Action nodes
            actions = new Dictionary<string, IBTAction>()
            {
                { "AttackEnemy", new AttackEnemy() },
                { "CalculateNextPathPoint", new CalculateNextPathPoint() },
                { "CancelGoAttack", new CancelGoAttack() },
                { "CancelGoToItem", new CancelGoToItem() },
                { "CheckForEnemies", new CheckForEnemies() },
                { "CheckForItemsInCruiser", new CheckForItemsInCruiser() },
                { "CheckForItemsInMap", new CheckForItemsInMap() },
                { "CheckForItemsInRange", new CheckForItemsInRange() },
                { "CheckForItemsNearGatheringPoint", new CheckForItemsNearGatheringPoint() },
                { "CheckLOSForClosestPlayer", new CheckLOSForClosestPlayer() },
                { "Chill", new Chill() },
                { "DropAllItems", new DropAllItems() },
                { "EnterVehicle", new EnterVehicle() },
                { "EquipWeapon", new EquipUnequipWeapon(equip: true) },
                { "ExitVehicle", new ExitVehicle() },
                { "FleeFromEnemy", new FleeFromEnemy() },
                { "GoToEnemy", new GoToEnemy() },
                { "GoToPosition", new GoToPosition() },
                { "GrabItemBehavior", new GrabItemBehavior() },
                { "InVehicle", new InVehicle() },
                { "LookingAround", new LookingAround() },
                { "LookingForPlayer", new LookingForPlayer() },
                { "ResetDestToCruiser", new ResetDestToCruiser() },
                { "UnequipWeapon", new EquipUnequipWeapon(equip: false) },
                { "UpdateDestCruiser", new UpdateDestCruiser() },
                { "UpdateDestPos", new UpdateDestPos() },
                { "UpdateLastKnownPos", new UpdateLastKnownPos() },
                { "VoiceScavenging", new VoiceScavenging() },
                { "WaitForCommand", new WaitForCommand() },
            };

            // Condition nodes
            conditions = new Dictionary<string, IBTCondition>()
            {
                { "AreFreeSlotsAvailable", new AreFreeSlotsAvailable() },
                { "CanAttackEnemy", new CanAttackEnemy() },
                { "HasItemAndInShip", new HasItemAndInShip() },
                { "IsAutoDefense", new IsAutoDefense() },
                { "IsCommandDropAllItemsInCruiser", new IsCommandThis(EnumCommandTypes.DropAllItemsInCruiser) },
                { "IsCommandDropAllItemsToPos", new IsCommandThis(new EnumCommandTypes[] { EnumCommandTypes.DropAllItemsToShip, EnumCommandTypes.DropAllItemsOnGatheringPoint }) },
                { "IsCommandFollowPlayer", new IsCommandThis(EnumCommandTypes.FollowPlayer) },
                { "IsCommandGoFetchItem", new IsCommandThis(EnumCommandTypes.GoFetchItem) },
                { "IsCommandGoToVehicle", new IsCommandThis(EnumCommandTypes.GoToVehicle) },
                { "IsCommandGoToPosition", new IsCommandThis(EnumCommandTypes.GoToPosition) },
                { "IsCommandScavengingMode", new IsCommandThis(new EnumCommandTypes[] { EnumCommandTypes.ScavengingToShip, EnumCommandTypes.ScavengingToCruiser, EnumCommandTypes.ScavengingToGatheringPoint }) },
                { "IsCommandScavengingToPos", new IsCommandThis(new EnumCommandTypes[] { EnumCommandTypes.ScavengingToShip, EnumCommandTypes.ScavengingToGatheringPoint }) },
                { "IsCommandScavengingToCruiser", new IsCommandThis(EnumCommandTypes.ScavengingToCruiser) },
                { "IsCommandUnloadCruiser", new IsCommandThis(EnumCommandTypes.UnloadCruiser) },
                { "IsCommandUnloadGatheringPoint", new IsCommandThis(EnumCommandTypes.UnloadGatheringPoint) },
                { "IsCommandWaitForCommand", new IsCommandThis(EnumCommandTypes.WaitForCommand) },
                { "IsCommandKill", new IsCommandThis(EnumCommandTypes.Kill) },
                { "IsInDanger", new IsInDanger() },
                { "IsInternInVehicle", new IsInternInVehicle() },
                { "IsLastKnownPositionValid", new IsLastKnownPositionValid() },
                { "IsStillInCombat", new IsStillInCombat() },
                { "IsTargetInVehicle", new IsTargetInVehicle() },
                { "IsTargetItemValid", new IsTargetItemValid() },
                { "TargetValid", new TargetValid() },
                { "TooFarFromCruiser", new TooFarFromCruiser() },
                { "TooFarFromEnemy", new TooFarFromEnemy() },
                { "TooFarFromObject", new TooFarFromObject() },
                { "TooFarFromPos", new TooFarFromPos() },
            };
        }

        private void InitContext(InternAI internAI)
        {
            BTContext = new BTContext()
            {
                InternAI = internAI,

                searchForPlayers = this.searchForPlayers,

                LookingAroundCoroutineController = CoroutineControllers[0],
                searchingWanderCoroutineController = CoroutineControllers[1],
                CalculatePathCoroutineController = CoroutineControllers[2],
                ChillCoroutine = CoroutineControllers[3],
            };
        }

        public void ResetContextNewCommandFollowPlayer()
        {
            _movingPlayerPoint.Transform = BTContext.InternAI.targetPlayer.transform;
            _movingPlayerPoint.Name = $"targetPlayer {BTContext.InternAI.targetPlayer.playerUsername}";

            BTContext.PathController.Reset();
            BTContext.PathfindingContext.SetDestination(_movingPlayerPoint.Clone(InternManager.Instance.Pools));
            BTContext.TargetItem = null;
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandToInterestPoint(IPointOfInterest pointOfInterest)
        {
            IInterestPoint? interestPoint = pointOfInterest.GetInterestPoint();
            if (interestPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("ResetContextNewCommandToInterestPoint interestPoint is null");
                return;
            }

            switch (interestPoint)
            {
                case GatheringInterestPoint gp:
                    _staticInterestPoint.Position = gp.Point;
                    _staticInterestPoint.Name = "Gathering point interest point";
                    _tempPoint = _staticInterestPoint;
                    break;
                case PositionInterestPoint pp:
                    _staticInterestPoint.Position = pp.Point;
                    _staticInterestPoint.Name = "Position interest point";
                    _tempPoint = _staticInterestPoint;
                    break;
                case ShipInterestPoint sp:
                    _staticInterestPoint.Position = sp.Point;
                    _staticInterestPoint.Name = "Ship interest point";
                    _tempPoint = _staticInterestPoint;
                    break;
                case VehicleInterestPoint vp:
                    _vehiclePoint.Transform = vp.VehicleTransform;
                    _tempPoint = _vehiclePoint;
                    break;
            }
            if (_tempPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("ResetContextNewCommandToInterestPoint _tempPoint is null");
                return;
            }

            BTContext.PathfindingContext.SetDestination(_tempPoint.Clone(InternManager.Instance.Pools));
            BTContext.PathController.Reset();
            BTContext.TargetItem = null;
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandToScavenging()
        {
            BTContext.TargetItem = null;
            BTContext.cancelScavenging = false;
            BTContext.PathController.Reset();
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandGoFetchItem(GrabbableObject itemToFetch)
        {
            BTContext.TargetItem = itemToFetch;

            _itemPoint.Transform = itemToFetch.transform;
            _itemPoint.GrabDistance = BTContext.InternAI.Npc.grabDistance * PluginRuntimeProvider.Context.Config.InternSizeScale;
            _itemPoint.SetName(itemToFetch);
            BTContext.PathfindingContext.SetDestination(_itemPoint.Clone(InternManager.Instance.Pools));
            BTContext.PathController.Reset();
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextAttackEnemy(EnemyAI enemy)
        {
            _movingEnemyPoint.Transform = enemy.transform;
            _movingEnemyPoint.Name = $"targetEnemy {enemy.enemyType.enemyName}";
            BTContext.PathController.Reset();
            BTContext.PathfindingContext.SetDestination(_movingEnemyPoint.Clone(InternManager.Instance.Pools));
            BTContext.CurrentEnemy = enemy;
            BTContext.TargetItem = null;
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandDropToPos(IPointOfInterest pointOfInterest)
        {
            IInterestPoint? interestPoint = pointOfInterest.GetInterestPoint();
            if (interestPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("ResetContextNewCommandDropToPos interestPoint is null");
                return;
            }

            BTContext.TargetItem = null;
            _staticInterestPoint.Position = interestPoint.Point;
            BTContext.PathfindingContext.SetDestination(_staticInterestPoint.Clone(InternManager.Instance.Pools));
            BTContext.PathController.Reset();
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandDropToCruiser()
        {
            BTContext.TargetItem = null;
            BTContext.PathController.Reset();
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandUnloadFromCruiser()
        {
            BTContext.TargetItem = null;
            BTContext.PathController.Reset();
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandUnloadFromPos(IPointOfInterest pointOfInterest)
        {
            IInterestPoint? interestPoint = pointOfInterest.GetInterestPoint();
            if (interestPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("ResetContextNewCommandDropToPos interestPoint is null");
                return;
            }

            BTContext.TargetItem = null;
            BTContext.PathController.Reset();
            _staticInterestPoint.Position = interestPoint.Point;
            BTContext.PathfindingContext.SetDestination(_staticInterestPoint.Clone(InternManager.Instance.Pools));
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }

        public void ResetContext()
        {
            BTContext.PathController.Reset();
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }

        public EnemyAI? GetTarget()
        {
            return BTContext.CurrentEnemy;
        }
        public GrabbableObject? GetTargetItem()
        {
            return BTContext.TargetItem;
        }

        private IBehaviourTreeNode CreateTree()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Panik or commands")

                    .Sequence("Command kill enemy ?")
                        .Condition("<IsCommandKill>", t => conditions["IsCommandKill"].Condition(BTContext))
                        .Selector("Can attack enemy or cancel ?")
                            .Sequence("Go attack if can")
                                .Condition("<CanAttackEnemy>", t => conditions["CanAttackEnemy"].Condition(BTContext))
                                .Splice(CreateSubTreeGoAttack())
                            .End()
                            .Do("CancelGoAttack", t => actions["CancelGoAttack"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Is in danger ?")
                        .Do("CheckForEnemies", t => actions["CheckForEnemies"].Action(BTContext))
                        .Condition("<IsInDanger>", t => conditions["IsInDanger"].Condition(BTContext))

                        .Selector("Attack or flee")
                            .Sequence("Attack if auto defense ok")
                                .Condition("<IsAutoDefense>", t => conditions["IsAutoDefense"].Condition(BTContext))
                                .Condition("<CanAttackEnemy>", t => conditions["CanAttackEnemy"].Condition(BTContext))
                                .Condition("<IsStillInCombat>", t => conditions["IsStillInCombat"].Condition(BTContext))
                                .Splice(CreateSubTreeGoAttack())
                            .End()
                            .Do("FleeFromEnemy", t => actions["FleeFromEnemy"].Action(BTContext))
                        .End()
                    .End()

                    .Sequence("Follow orders")
                        .Do("UnequipWeapon", t => actions["UnequipWeapon"].Action(BTContext))
                        .Selector("Check commands")

                            .Sequence("Command wait for commands")
                                .Condition("<isCommand WaitForCommand>", t => conditions["IsCommandWaitForCommand"].Condition(BTContext))
                                .Do("WaitForCommand", t => actions["WaitForCommand"].Action(BTContext))
                            .End()

                            .Sequence("Command go to position")
                                .Condition("<isCommand GoToPosition>", t => conditions["IsCommandGoToPosition"].Condition(BTContext))
                                .Selector("Go to position")
                                    .Splice(CreateSubTreeGoToPosition())
                                    .Sequence("Drop item if in ship")
                                        .Condition("<HasItemAndInShip>", t => conditions["HasItemAndInShip"].Condition(BTContext))
                                        .Do("DropAllItems", t => actions["DropAllItems"].Action(BTContext))
                                    .End()
                                    .Do("Chill", t => actions["Chill"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Command go to vehicle")
                                .Condition("<isCommand GoToVehicle>", t => conditions["IsCommandGoToVehicle"].Condition(BTContext))
                                .Splice(CreateSubTreeGoToVehicle(actionInCruiser: "InVehicle"))
                            .End()

                            .Sequence("Command GoFetchItem")
                                .Condition("<isCommand GoFetchItem>", t => conditions["IsCommandGoFetchItem"].Condition(BTContext))
                                .Splice(CreateSubTreeGoFetchItem())
                            .End()

                            .Sequence("Command drop items to position")
                                .Condition("<IsCommandDropAllItemsToPos>", t => conditions["IsCommandDropAllItemsToPos"].Condition(BTContext))
                                .Selector("Go to position or drop object")
                                    .Splice(CreateSubTreeGoToPosition())
                                    .Do("DropAllItems", t => actions["DropAllItems"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Command drop items in cruiser")
                                .Condition("<IsCommandDropAllItemsInCruiser>", t => conditions["IsCommandDropAllItemsInCruiser"].Condition(BTContext))
                                .Splice(CreateSubTreeGoToVehicle(actionInCruiser: "DropAllItems"))
                            .End()

                            .Sequence("Fetch object casually")
                                .Condition("<AreFreeSlotsAvailable>", t => conditions["AreFreeSlotsAvailable"].Condition(BTContext))
                                .Do("CheckForItemsInRange", t => actions["CheckForItemsInRange"].Action(BTContext))
                                .Condition("<IsTargetItemValid>", t => conditions["IsTargetItemValid"].Condition(BTContext))
                                .Selector("Should go to item")
                                    .Splice(CreateSubTreeGoToObject())
                                    .Do("GrabObject", t => actions["GrabItemBehavior"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Command follow player")
                                .Condition("<isCommand FollowPlayer>", t => conditions["IsCommandFollowPlayer"].Condition(BTContext))
                                .Condition("<TargetValid>", t => conditions["TargetValid"].Condition(BTContext))
                                .Splice(CreateSubTreeFollowPlayer())
                            .End()

                            .Sequence("Command Unload from cruiser")
                                .Condition("<IsCommandUnloadCruiser>", t => conditions["IsCommandUnloadCruiser"].Condition(BTContext))
                                .Splice(CreateSubTreeUnloadCruiser())
                            .End()

                            .Sequence("Command Unload from gathering point")
                                .Condition("<IsCommandUnloadGatheringPoint>", t => conditions["IsCommandUnloadGatheringPoint"].Condition(BTContext))
                                .Splice(CreateSubTreeUnloadGatheringPoint())
                            .End()

                            .Sequence("Command scavenging")
                                .Condition("<isCommand ScavengingMode>", t => conditions["IsCommandScavengingMode"].Condition(BTContext))
                                .Splice(CreateSubTreeScavenging())
                            .End()

                        //.Do("checkLOSForClosestPlayer", t => actions["CheckLOSForClosestPlayer"].Action(BTContext))
                        //.Do("LookingForPlayer", t => actions["LookingForPlayer"].Action(BTContext))

                        .End() // Selector("Check commands")
                    .End() // .Sequence("Follow orders")
                .End() // Selector("Panik or commands")
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeExitVehicle()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Sequence("Intern in vehicle, exit")
                            .Condition("<isInternInVehicle>", t => conditions["IsInternInVehicle"].Condition(BTContext))
                            .Do("exitVehicle", t => actions["ExitVehicle"].Action(BTContext))
                        .End()
                   .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToPosition()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Exit vehicle or go to position")
                    .Splice(CreateSubTreeExitVehicle())

                    .Sequence("Go to position if too far")
                        .Condition("<tooFarFromPos>", t => conditions["TooFarFromPos"].Condition(BTContext))
                        .Do("CalculateNextPathPoint", t => actions["CalculateNextPathPoint"].Action(BTContext))
                        .Do("goToPosition", t => actions["GoToPosition"].Action(BTContext))
                    .End()
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToObject()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Go to position")
                    .Splice(CreateSubTreeExitVehicle())

                    .Sequence("Go to position")
                        .Condition("<tooFarFromObject>", t => conditions["TooFarFromObject"].Condition(BTContext))
                        .Do("CalculateNextPathPoint", t => actions["CalculateNextPathPoint"].Action(BTContext))
                        .Do("goToPosition", t => actions["GoToPosition"].Action(BTContext))
                    .End()
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoToVehicle(string actionInCruiser)
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Sequence("Go to cruiser")
                    .Do("UpdateDestCruiser", t => actions["UpdateDestCruiser"].Action(BTContext))
                    .Selector("Enter cruiser")
                        .Sequence(actionInCruiser)
                            .Condition("<isInternInVehicle>", t => conditions["IsInternInVehicle"].Condition(BTContext))
                            .Do(actionInCruiser, t => actions[actionInCruiser].Action(BTContext))
                        .End()
                        .Sequence("Go to position if too far")
                            .Condition("<tooFarFromPos>", t => conditions["TooFarFromPos"].Condition(BTContext))
                            .Do("CalculateNextPathPoint", t => actions["CalculateNextPathPoint"].Action(BTContext))
                            .Do("goToPosition", t => actions["GoToPosition"].Action(BTContext))
                        .End()
                        .Sequence("Too far from cruiser ?")
                            .Condition("<TooFarFromCruiser>", t => conditions["TooFarFromCruiser"].Condition(BTContext))
                            .Do("ResetDestToCruiser", t => actions["ResetDestToCruiser"].Action(BTContext))
                        .End()
                        .Do("EnterVehicle", t => actions["EnterVehicle"].Action(BTContext))
                    .End()
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
                        .Splice(CreateSubTreeGoToVehicle(actionInCruiser: "InVehicle"))
                    .End()

                    .Sequence("Follow player")
                        // no use for update last known pos, even with config, it just not work for now
                        .Do("updateLastKnownPos", t => actions["UpdateLastKnownPos"].Action(BTContext))
                        .Selector("Go to pos or chill")
                            .Splice(CreateSubTreeGoToPosition())
                            .Sequence("Drop item if in ship")
                                .Condition("<HasItemAndInShip>", t => conditions["HasItemAndInShip"].Condition(BTContext))
                                .Do("DropAllItems", t => actions["DropAllItems"].Action(BTContext))
                            .End()
                            .Do("chill", t => actions["Chill"].Action(BTContext))
                        .End()
                    .End()

                    .Do("LookingAround", t => actions["LookingAround"].Action(BTContext))
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoAttack()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Go to enemy or attack")
                            .Sequence("Attack if auto defense ok")
                                .Do("EquipWeapon", t => actions["EquipWeapon"].Action(BTContext))
                                .Condition("<TooFarFromEnemy>", t => conditions["TooFarFromEnemy"].Condition(BTContext))
                                .Do("GoToEnemy", t => actions["GoToEnemy"].Action(BTContext))
                            .End()
                            .Do("AttackEnemy", t => actions["AttackEnemy"].Action(BTContext))
                        .End()
                        .Build();
        }

        private IBehaviourTreeNode CreateSubTreeScavenging()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Return to drop location or scavenge ?")
                            .Sequence("Look for items if hands free")
                                .Condition("<AreFreeSlotsAvailable>", t => conditions["AreFreeSlotsAvailable"].Condition(BTContext))
                                .Selector("Cancel scavenging ?")
                                    .Sequence("Intern in vehicle, exit")
                                        .Condition("<isInternInVehicle>", t => conditions["IsInternInVehicle"].Condition(BTContext))
                                        .Do("exitVehicle", t => actions["ExitVehicle"].Action(BTContext))
                                    .End()
                                    .Sequence("Go grab if item found")
                                        .Do("CheckForItemsInMap", t => actions["CheckForItemsInMap"].Action(BTContext))
                                        .Condition("<IsTargetItemValid>", t => conditions["IsTargetItemValid"].Condition(BTContext))
                                        .Do("VoiceScavenging", t => actions["VoiceScavenging"].Action(BTContext))
                                        .Selector("Go to object or grab")
                                            .Splice(CreateSubTreeGoToObject())
                                            .Do("GrabObject", t => actions["GrabItemBehavior"].Action(BTContext))
                                        .End()
                                    .End()
                                    .Do("CancelGoToItem", t => actions["CancelGoToItem"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Drop to drop location")
                                .Do("VoiceScavenging", t => actions["VoiceScavenging"].Action(BTContext))

                                .Selector("Drop location to where ?")
                                    .Sequence("Drop location position")
                                        .Condition("<IsCommandScavengingToPos>", t => conditions["IsCommandScavengingToPos"].Condition(BTContext))
                                        .Do("UpdateDestPos", t => actions["UpdateDestPos"].Action(BTContext))
                                        .Selector("Go to position or drop object")
                                            .Splice(CreateSubTreeGoToPosition())
                                            .Do("DropAllItems", t => actions["DropAllItems"].Action(BTContext))
                                        .End()
                                    .End()

                                    .Sequence("Drop location cruiser")
                                        .Condition("<IsCommandScavengingToCruiser>", t => conditions["IsCommandScavengingToCruiser"].Condition(BTContext))
                                        .Splice(CreateSubTreeGoToVehicle(actionInCruiser: "DropAllItems"))
                                    .End()
                                .End()
                            .End()
                        .End()
                        .Build();
        }

        private IBehaviourTreeNode CreateSubTreeGoFetchItem()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Item valid or cancel ?")
                            .Sequence("Go fetch if hands free")
                                .Condition("<AreFreeSlotsAvailable>", t => conditions["AreFreeSlotsAvailable"].Condition(BTContext))
                                .Condition("<IsTargetItemValid>", t => conditions["IsTargetItemValid"].Condition(BTContext))
                                .Selector("Go to object or grab")
                                    .Splice(CreateSubTreeGoToObject())
                                    .Do("GrabObject", t => actions["GrabItemBehavior"].Action(BTContext))
                                .End()
                            .End()

                            .Do("CancelGoToItem", t => actions["CancelGoToItem"].Action(BTContext))
                        .End()
                        .Build();
        }

        private IBehaviourTreeNode CreateSubTreeUnloadCruiser()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Return to drop location or scavenge ?")
                            .Sequence("Look for items if hands free")
                                .Condition("<AreFreeSlotsAvailable>", t => conditions["AreFreeSlotsAvailable"].Condition(BTContext))
                                .Selector("Cancel scavenging ?")
                                    .Sequence("Go grab if item found")
                                        .Do("CheckForItemsInCruiser", t => actions["CheckForItemsInCruiser"].Action(BTContext))
                                        .Condition("<IsTargetItemValid>", t => conditions["IsTargetItemValid"].Condition(BTContext))
                                        .Do("VoiceScavenging", t => actions["VoiceScavenging"].Action(BTContext))
                                        .Splice(CreateSubTreeGoToVehicle(actionInCruiser: "GrabItemBehavior"))
                                    .End()
                                    .Do("CancelGoToItem", t => actions["CancelGoToItem"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Drop to drop location")
                                .Do("VoiceScavenging", t => actions["VoiceScavenging"].Action(BTContext))
                                .Do("UpdateDestPos", t => actions["UpdateDestPos"].Action(BTContext))
                                .Selector("Go to position or drop object")
                                    .Splice(CreateSubTreeGoToPosition())
                                    .Do("DropAllItems", t => actions["DropAllItems"].Action(BTContext))
                                .End()
                            .End()
                        .End()
                        .Build();
        }

        private IBehaviourTreeNode CreateSubTreeUnloadGatheringPoint()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Return to drop location or scavenge ?")
                            .Sequence("Look for items if hands free")
                                .Condition("<AreFreeSlotsAvailable>", t => conditions["AreFreeSlotsAvailable"].Condition(BTContext))
                                .Selector("Cancel scavenging ?")
                                    .Sequence("Go grab if item found")
                                        .Do("CheckForItemsNearGatheringPoint", t => actions["CheckForItemsNearGatheringPoint"].Action(BTContext))
                                        .Condition("<IsTargetItemValid>", t => conditions["IsTargetItemValid"].Condition(BTContext))
                                        .Do("VoiceScavenging", t => actions["VoiceScavenging"].Action(BTContext))
                                        .Do("UpdateDestPos", t => actions["UpdateDestPos"].Action(BTContext))
                                        .Selector("Go to position or drop object")
                                            .Splice(CreateSubTreeGoToPosition())
                                            .Do("GrabItemBehavior", t => actions["GrabItemBehavior"].Action(BTContext))
                                        .End()
                                    .End()
                                    .Do("CancelGoToItem", t => actions["CancelGoToItem"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Drop to drop location")
                                .Do("VoiceScavenging", t => actions["VoiceScavenging"].Action(BTContext))
                                .Do("UpdateDestPos", t => actions["UpdateDestPos"].Action(BTContext))
                                .Selector("Go to position or drop object")
                                    .Splice(CreateSubTreeGoToPosition())
                                    .Do("DropAllItems", t => actions["DropAllItems"].Action(BTContext))
                                .End()
                            .End()
                        .End()
                        .Build();
        }

    }
}

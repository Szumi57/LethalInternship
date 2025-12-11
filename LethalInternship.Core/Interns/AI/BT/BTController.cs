using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.BT.ActionNodes;
using LethalInternship.Core.Interns.AI.BT.ConditionNodes;
using LethalInternship.Core.Interns.AI.CoroutineControllers;
using LethalInternship.Core.Interns.AI.Dijkstra;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Managers;
using LethalInternship.Core.Utils;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
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
                { "CancelGoToItem", new CancelGoToItem() },
                { "CheckForItemsInMap", new CheckForItemsInMap() },
                { "CheckForItemsInRange", new CheckForItemsInRange() },
                { "CheckLOSForClosestPlayer", new CheckLOSForClosestPlayer() },
                { "Chill", new Chill() },
                { "DropItem", new DropItem() },
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
                { "SetNextDestToShip", new SetNextDestToShip() },
                { "UnequipWeapon", new EquipUnequipWeapon(equip: false) },
                { "UpdateLastKnownPos", new UpdateLastKnownPos() },
                { "VoiceScavengingNoLoot", new VoiceScavengingNoLoot() },
                { "VoiceScavengingWithLoot", new VoiceScavengingWithLoot() },
            };

            // Condition nodes
            conditions = new Dictionary<string, IBTCondition>()
            {
                { "AreFreeSlotsAvailable", new AreFreeSlotsAvailable() },
                { "EnemySeen", new EnemySeen() },
                { "HasItemAndInShip", new HasItemAndInShip() },
                { "IsAutoDefense", new IsAutoDefense() },
                { "IsCommandFollowPlayer", new IsCommandThis(EnumCommandTypes.FollowPlayer) },
                { "IsCommandGoToVehicle", new IsCommandThis(EnumCommandTypes.GoToVehicle) },
                { "IsCommandGoToPosition", new IsCommandThis(EnumCommandTypes.GoToPosition) },
                { "IsCommandWait", new IsCommandThis(EnumCommandTypes.Wait) },
                { "IsCommandScavengingMode", new IsCommandThis(EnumCommandTypes.ScavengingMode) },
                { "IsInternInVehicle", new IsInternInVehicle() },
                { "IsLastKnownPositionValid", new IsLastKnownPositionValid() },
                { "IsTargetInVehicle", new IsTargetInVehicle() },
                { "IsTargetItemValid", new IsTargetItemValid() },
                { "TargetValid", new TargetValid() },
                { "TooFarFromEnemy", new TooFarFromEnemy() },
                { "TooFarFromObject", new TooFarFromObject() },
                { "TooFarFromPos", new TooFarFromPos() },
                { "TooFarFromVehicle", new TooFarFromVehicle() }
            };
        }

        private void InitContext(InternAI internAI)
        {
            DJKPointMapper mapper = new DJKPointMapper();
            mapper.Register<DefaultInterestPoint>(ip => new DJKStaticPoint(ip.Point));
            mapper.Register<ShipInterestPoint>(ip => new DJKStaticPoint(ip.Point));
            mapper.Register<VehicleInterestPoint>(ip => new DJKVehiclePoint(ip.VehicleTransform, "Cruiser"));

            BTContext = new BTContext()
            {
                InternAI = internAI,

                PathController = new PathController(),
                DJKPointMapper = mapper,

                searchForPlayers = this.searchForPlayers,

                LookingAroundCoroutineController = CoroutineControllers[0],
                PanikCoroutine = CoroutineControllers[1],
                searchingWanderCoroutineController = CoroutineControllers[2],
                CalculatePathCoroutineController = CoroutineControllers[3],
            };
        }

        public void ResetContextNewCommandFollowPlayer()
        {
            BTContext.PathController.ResetPathAndIndex();
            BTContext.PathController.SetNewDestination(new DJKMovingPoint(BTContext.InternAI.targetPlayer.transform, $"targetPlayer {BTContext.InternAI.targetPlayer.playerUsername}"));
            BTContext.TargetItem = null;
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandToInterestPoint(IPointOfInterest pointOfInterest)
        {
            IInterestPoint? interestPoint = pointOfInterest.GetInterestPoint();
            if (interestPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("SetNextDestInterestPoint interestPoint is null");
                return;
            }

            BTContext.PathController.SetNewDestination(BTContext.DJKPointMapper.Map(interestPoint));
            BTContext.TargetItem = null;
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }
        public void ResetContextNewCommandToScavenging()
        {
            BTContext.TargetItem = null;
            BTContext.cancelScavenging = false;
            InternManager.Instance.CancelBatch((int)BTContext.InternAI.Npc.playerClientId);
        }

        public EnemyAI? GetTarget()
        {
            return BTContext.CurrentEnemy;
        }

        private IBehaviourTreeNode CreateTree()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Panik or commands")

                    .Sequence("Enemy close ?")
                        .Condition("<EnemySeen>", t => conditions["EnemySeen"].Condition(BTContext))
                        .Splice(CreateSubTreePanik())
                    .End()

                    .Sequence("Follow orders")
                        .Do("UnequipWeapon", t => actions["UnequipWeapon"].Action(BTContext))
                        .Selector("Check commands")
                            .Sequence("Command go to position")
                                .Condition("<isCommand GoToPosition>", t => conditions["IsCommandGoToPosition"].Condition(BTContext))
                                .Selector("Go to position")
                                    .Splice(CreateSubTreeGoToPosition())
                                    .Sequence("Drop item if in ship")
                                        .Condition("<HasItemAndInShip>", t => conditions["HasItemAndInShip"].Condition(BTContext))
                                        .Do("DropItem", t => actions["DropItem"].Action(BTContext))
                                    .End()
                                    .Do("Chill", t => actions["Chill"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Command go to vehicle")
                                .Condition("<isCommand GoToVehicle>", t => conditions["IsCommandGoToVehicle"].Condition(BTContext))
                                .Splice(CreateSubTreeGoToVehicle())
                            .End()

                            .Sequence("Fetch object")
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

                            .Sequence("Command scavenging")
                                .Condition("<isCommand ScavengingMode>", t => conditions["IsCommandScavengingMode"].Condition(BTContext))
                                .Splice(CreateSubScavenging())
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

        private IBehaviourTreeNode CreateSubTreeGoToVehicle()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                .Selector("Go to vehicle")
                    .Sequence("In vehicle")
                        .Condition("<isInternInVehicle>", t => conditions["IsInternInVehicle"].Condition(BTContext))
                        .Do("inVehicle", t => actions["InVehicle"].Action(BTContext))
                    .End()

                    .Sequence("Go to vehicle")
                        .Condition("<tooFarFromVehicle>", t => conditions["TooFarFromVehicle"].Condition(BTContext))
                        .Do("CalculateNextPathPoint", t => actions["CalculateNextPathPoint"].Action(BTContext))
                        .Do("goToPosition", t => actions["GoToPosition"].Action(BTContext))
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

                    .Sequence("Follow player")
                        // no use for update last known pos, even with config, it just not work for now
                        .Do("updateLastKnownPos", t => actions["UpdateLastKnownPos"].Action(BTContext))
                        .Selector("Go to pos or chill")
                            .Splice(CreateSubTreeGoToPosition())
                            .Sequence("Drop item if in ship")
                                .Condition("<HasItemAndInShip>", t => conditions["HasItemAndInShip"].Condition(BTContext))
                                .Do("DropItem", t => actions["DropItem"].Action(BTContext))
                            .End()
                            .Do("chill", t => actions["Chill"].Action(BTContext))
                        .End()
                    .End()

                    .Do("LookingAround", t => actions["LookingAround"].Action(BTContext))
                .End()
                .Build();
        }

        private IBehaviourTreeNode CreateSubTreePanik()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Auto defense or flee")
                            .Sequence("Attack if auto defense ok")
                                .Condition("<IsAutoDefense>", t => conditions["IsAutoDefense"].Condition(BTContext))
                                .Selector("Go to enemy of attack")
                                    .Sequence("Attack if auto defense ok")
                                        .Do("EquipWeapon", t => actions["EquipWeapon"].Action(BTContext))
                                        .Condition("<TooFarFromEnemy>", t => conditions["TooFarFromEnemy"].Condition(BTContext))
                                        .Do("GoToEnemy", t => actions["GoToEnemy"].Action(BTContext))
                                    .End()
                                    .Do("AttackEnemy", t => actions["AttackEnemy"].Action(BTContext))
                                .End()
                            .End()
                            .Do("FleeFromEnemy", t => actions["FleeFromEnemy"].Action(BTContext))
                        .End()
                        .Build();
        }

        private IBehaviourTreeNode CreateSubScavenging()
        {
            var builder = new BehaviourTreeBuilder();
            return builder
                        .Selector("Return to ship or scavenge ?")
                            .Sequence("Look for items if hands free")
                                .Condition("<AreFreeSlotsAvailable>", t => conditions["AreFreeSlotsAvailable"].Condition(BTContext))
                                .Do("CheckForItemsInMap", t => actions["CheckForItemsInMap"].Action(BTContext))
                                .Selector("Cancel scavenging ?")
                                    .Sequence("Go grab if item found")
                                        .Condition("<IsTargetItemValid>", t => conditions["IsTargetItemValid"].Condition(BTContext))
                                        .Do("VoiceScavengingNoLoot", t => actions["VoiceScavengingNoLoot"].Action(BTContext))
                                        .Selector("Go to object or grab")
                                            .Splice(CreateSubTreeGoToObject())
                                            .Do("GrabObject", t => actions["GrabItemBehavior"].Action(BTContext))
                                        .End()
                                    .End()
                                    .Do("CancelGoToItem", t => actions["CancelGoToItem"].Action(BTContext))
                                .End()
                            .End()

                            .Sequence("Return to ship")
                                .Do("Set next point to ship", t => actions["SetNextDestToShip"].Action(BTContext))
                                .Do("VoiceScavengingWithLoot", t => actions["VoiceScavengingWithLoot"].Action(BTContext))
                                .Selector("Go to position or drop object")
                                    .Splice(CreateSubTreeGoToPosition())
                                    .Do("DropItem", t => actions["DropItem"].Action(BTContext))
                                .End()
                            .End()
                        .End()
                        .Build();
        }
    }
}

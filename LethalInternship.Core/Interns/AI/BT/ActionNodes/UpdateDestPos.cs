using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class UpdateDestPos : IBTAction
    {
        private DJKStaticPoint _shipStaticPoint = new DJKStaticPoint("Ship drop location");
        private DJKStaticPoint _gatheringPointStaticPoint = new DJKStaticPoint("GatheringPoint");

        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;
            Transform? shipTransform = InternManager.Instance.ShipTransform;
            IPointOfInterest? gatheringPoint = InternManager.Instance.GatheringPoint;
            IInterestPoint? gatheringIP = null;

            switch (ai.CurrentCommand)
            {
                case EnumCommandTypes.ScavengingToShip:
                case EnumCommandTypes.UnloadCruiser:
                    if (shipTransform == null)
                    {
                        PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation shipTransform not found !");
                        return BehaviourTreeStatus.Failure;
                    }
                    _shipStaticPoint.Position = ShipInterestPoint.GetShipPoint(shipTransform);
                    context.FinalDestination = _shipStaticPoint;

                    break;
                case EnumCommandTypes.ScavengingToGatheringPoint:
                    if (gatheringPoint == null)
                    {
                        PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation ScavengingToGatheringPoint gatheringPoint not found !");
                        return BehaviourTreeStatus.Failure;
                    }
                    gatheringIP = gatheringPoint.GetInterestPoint();
                    if (gatheringIP == null)
                    {
                        PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation ScavengingToGatheringPoint gatheringIP not found !");
                        return BehaviourTreeStatus.Failure;
                    }

                    _gatheringPointStaticPoint.Position = gatheringIP.Point;
                    context.FinalDestination = _gatheringPointStaticPoint;
                    break;
                case EnumCommandTypes.UnloadGatheringPoint:
                    if (ai.AreFreeSlotsAvailable())
                    {
                        // Still got space to hold items
                        if (gatheringPoint == null)
                        {
                            PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation UnloadGatheringPoint no gathering point set !");
                            return BehaviourTreeStatus.Failure;
                        }
                        gatheringIP = gatheringPoint.GetInterestPoint();
                        if (gatheringIP == null)
                        {
                            PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation UnloadGatheringPoint gatheringIP not found !");
                            return BehaviourTreeStatus.Failure;
                        }
                        _gatheringPointStaticPoint.Position = gatheringIP.Point;
                        context.FinalDestination = _gatheringPointStaticPoint;
                    }
                    else
                    {
                        // UnloadGatheringPoint and hands full
                        if (shipTransform == null)
                        {
                            PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation shipTransform not found !");
                            return BehaviourTreeStatus.Failure;
                        }
                        _shipStaticPoint.Position = ShipInterestPoint.GetShipPoint(shipTransform);
                        context.FinalDestination = _shipStaticPoint;
                    }

                    break;
            }

            context.PathfindingContext.SetDestination(context.FinalDestination.Clone(InternManager.Instance.Pools));
            return BehaviourTreeStatus.Success;
        }
    }
}

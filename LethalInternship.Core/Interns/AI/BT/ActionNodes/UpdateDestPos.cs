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
            IPointOfInterest? gatheringPoint = InternManager.Instance.GatheringPoint;

            Transform? shipTransform = InternManager.Instance.ShipTransform;
            if (shipTransform == null)
            {
                PluginLoggerHook.LogError?.Invoke("UpdateDestPos shipTransform not found !");
                return BehaviourTreeStatus.Failure;
            }

            switch (ai.CurrentCommand)
            {
                case EnumCommandTypes.ScavengingToShip:
                case EnumCommandTypes.UnloadCruiser:

                    SetShipDestination(context, shipTransform);
                    break;
                case EnumCommandTypes.ScavengingToGatheringPoint:
                case EnumCommandTypes.UnloadGatheringPoint:

                    if (ai.CurrentCommand == EnumCommandTypes.UnloadGatheringPoint
                        && context.TargetItem == null
                        && !ai.AreHandsFree())
                    {
                        SetShipDestination(context, shipTransform);
                        break;
                    }

                    var gatheringIP = gatheringPoint?.GetInterestPoint();
                    if (gatheringIP == null)
                    {
                        SetShipDestination(context, shipTransform);
                        break;
                    }

                    _gatheringPointStaticPoint.Position = gatheringIP.Point;
                    context.PathfindingContext.SetDestination(_gatheringPointStaticPoint.Clone(InternManager.Instance.Pools));
                    break;
            }

            return BehaviourTreeStatus.Success;
        }

        private void SetShipDestination(BTContext context, Transform shipTransform)
        {
            _shipStaticPoint.Position = ShipInterestPoint.GetShipPoint(shipTransform);
            context.PathfindingContext.SetDestination(_shipStaticPoint.Clone(InternManager.Instance.Pools));
        }
    }
}

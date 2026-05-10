using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class UpdateDestPos : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            switch (ai.CurrentCommand)
            {
                case EnumCommandTypes.ScavengingToShip:
                case EnumCommandTypes.DropAllItemsToShip:
                case EnumCommandTypes.UnloadCruiser:
                case EnumCommandTypes.UnloadGatheringPoint:
                    Transform? shipTransform = InternManager.Instance.ShipTransform;
                    if (shipTransform == null)
                    {
                        PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation shipTransform not found !");
                        return BehaviourTreeStatus.Failure;
                    }
                    context.PathController.SetNewDestination(new DJKStaticPoint(ShipInterestPoint.GetShipPoint(shipTransform), $"Ship drop location"));

                    break;
                case EnumCommandTypes.ScavengingToGatheringPoint:

                    Debug.Log("SetNextDestToDropLocation EnumDropLocation.GatheringPoint not implemented !!!!!!!!!!!!!");
                    break;
            }

            return BehaviourTreeStatus.Success;
        }
    }
}

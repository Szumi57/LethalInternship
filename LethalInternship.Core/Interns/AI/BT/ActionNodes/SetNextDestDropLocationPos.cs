using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestDropLocationPos : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            switch (ai.CurrentCommand)
            {
                case SharedAbstractions.Enums.EnumCommandTypes.ScavengingToShip:
                    Transform? shipTransform = InternManager.Instance.ShipTransform;
                    if (shipTransform == null)
                    {
                        PluginLoggerHook.LogError?.Invoke("SetNextDestToDropLocation shipTransform not found !");
                        return BehaviourTreeStatus.Failure;
                    }
                    context.PathController.SetNewDestination(new DJKStaticPoint(ShipInterestPoint.GetShipPoint(shipTransform), $"Ship drop location"));

                    break;
                case SharedAbstractions.Enums.EnumCommandTypes.ScavengingToGatheringPoint:

                    Debug.Log("SetNextDestToDropLocation EnumDropLocation.GatheringPoint not implemented !!!!!!!!!!!!!");
                    break;
            }

            return BehaviourTreeStatus.Success;
        }
    }
}

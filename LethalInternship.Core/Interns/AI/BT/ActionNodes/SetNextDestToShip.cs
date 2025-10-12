using LethalInternship.Core.BehaviorTree;
using LethalInternship.Core.Interns.AI.Dijkstra.DJKPoints;
using LethalInternship.Core.Interns.AI.PointsOfInterest.InterestPoints;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestToShip : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            Transform? shipTransform = GameObject.Find("HangarShip").GetComponent<Transform>();
            if (shipTransform == null)
            {
                PluginLoggerHook.LogError?.Invoke("SetNextDestToShip shipTransform not found !");
                return BehaviourTreeStatus.Failure;
            }

            context.PathController.SetNewDestination(new DJKStaticPoint(ShipInterestPoint.GetShipPoint(shipTransform), $"Ship"));

            return BehaviourTreeStatus.Success;
        }
    }
}

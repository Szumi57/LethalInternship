using LethalInternship.Core.BehaviorTree;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.Interns.AI.BT.ActionNodes
{
    public class SetNextDestInterestPoint : IBTAction
    {
        public BehaviourTreeStatus Action(BTContext context)
        {
            InternAI ai = context.InternAI;

            IPointOfInterest? pointOfInterest = ai.GetPointOfInterest();
            if (pointOfInterest == null)
            {
                PluginLoggerHook.LogError?.Invoke("SetNextDestInterestPoint pointOfInterest is null");
                return BehaviourTreeStatus.Failure;
            }

            IInterestPoint? interestPoint = pointOfInterest.GetInterestPoint();
            if (interestPoint == null)
            {
                PluginLoggerHook.LogError?.Invoke("SetNextDestInterestPoint interestPoint is null");
                return BehaviourTreeStatus.Failure;
            }

            context.PathController.SetNewDestination(context.DJKPointMapper.Map(interestPoint));

            return BehaviourTreeStatus.Success;
        }
    }
}

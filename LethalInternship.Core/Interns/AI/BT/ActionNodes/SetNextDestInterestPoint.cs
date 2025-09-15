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
            if (pointOfInterest != null)
            {
                context.PathController.SetNewDestination(ai.transform.position, pointOfInterest.GetPoint());
            }

            return BehaviourTreeStatus.Success;
        }
    }
}

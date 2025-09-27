using LethalInternship.Core.BehaviorTree;
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
                context.PathController.SetNewDestinationPositionPoint(pointOfInterest.GetPoint(), "InterestPoint");
            }

            return BehaviourTreeStatus.Success;
        }
    }
}

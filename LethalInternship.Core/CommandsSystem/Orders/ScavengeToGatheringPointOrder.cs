using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class ScavengeToGatheringPointOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            // Give order
            intern.SetCommandToScavengingToGatheringPoint();
        }
    }
}

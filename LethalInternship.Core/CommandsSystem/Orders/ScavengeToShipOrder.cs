using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class ScavengeToShipOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            // Give order
            intern.SetCommandToScavenging();
        }
    }
}

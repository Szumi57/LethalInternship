using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class WaitForCommandOrder : Order
    {
        private bool wait;

        public WaitForCommandOrder(bool wait)
        {
            this.wait = wait;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandToWaitForCommand(wait);
        }
    }
}

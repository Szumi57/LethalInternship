using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class FollowMeOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            intern.SetCommandToFollowPlayer();
        }
    }
}

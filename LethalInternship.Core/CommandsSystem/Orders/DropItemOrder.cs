using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class DropItemOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            GrabbableObject? grabbableObject = intern.GetCurrentlyHeldItem();
            if (grabbableObject != null)
            {
                intern.DropItem(grabbableObject);
            }
        }
    }
}

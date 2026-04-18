using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class GoFetchItemOrder : Order
    {
        private readonly GrabbableObject? itemToFetch;

        public GoFetchItemOrder(GrabbableObject? item)
        {
            itemToFetch = item;
        }

        public override void ApplyTo(IInternAI intern)
        {
            if (itemToFetch == null)
                intern.SetCommandToFollowPlayer();
            else
                intern.SetCommandToFetchItem(itemToFetch);
        }
    }
}

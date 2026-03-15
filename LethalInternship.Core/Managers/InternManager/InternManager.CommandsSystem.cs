using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public void ExecuteOrder(Order order)
        {
            IInternAI[] internsOwned = GetInternsAIOwnedByLocal();
            foreach (IInternAI intern in internsOwned)
            {
                intern.AssignOrder(order);
            }
        }
    }
}

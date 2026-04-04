using LethalInternship.Core.CommandsSystem;
using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Linq;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        public void ExecuteOrder(Order order)
        {
            var internsOwned = IdentitySelectionService.Instance.SelectedInterns.Select(x => x.InternAI);
            foreach (IInternAI? intern in internsOwned)
            {
                if (intern == null)
                    continue;

                intern.AssignOrder(order);
            }
        }
    }
}

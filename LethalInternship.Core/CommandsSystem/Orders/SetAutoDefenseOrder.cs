using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class SetAutoDefenseOrder : Order
    {
        private bool _autoDefense;

        public SetAutoDefenseOrder(bool autoDefense)
        {
            _autoDefense = autoDefense;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.InternIdentity.SetAutoDefense(_autoDefense);
        }
    }
}

using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class DropAllItemsOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            intern.DropAllItems(EnumOptionsGetItems.IgnoreWeapon);
        }
    }
}

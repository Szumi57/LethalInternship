using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class ChangePreviousSuitOrder : Order
    {
        public ChangePreviousSuitOrder(IReadOnlyList<IInternIdentity> identities) : base(identities)
        {
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.ChangeSuitInternServerRpc(intern.Npc.playerClientId, intern.InternIdentity.GetPreviousSuitID());
        }
    }
}

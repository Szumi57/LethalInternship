using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class ChangeNextSuitOrder : Order
    {
        public override void ApplyTo(IInternAI intern)
        {
            intern.ChangeSuitInternServerRpc(intern.Npc.playerClientId, intern.InternIdentity.GetNextSuitID());
        }
    }
}

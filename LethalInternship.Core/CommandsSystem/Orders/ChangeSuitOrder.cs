using LethalInternship.SharedAbstractions.CommandsSystem;
using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.Core.CommandsSystem.Orders
{
    public class ChangeSuitOrder : Order
    {
        private int suitId;

        public ChangeSuitOrder(int suitId)
        {
            this.suitId = suitId;
        }

        public override void ApplyTo(IInternAI intern)
        {
            intern.ChangeSuitInternServerRpc(intern.Npc.playerClientId, this.suitId);
        }
    }
}

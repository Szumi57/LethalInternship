using LethalInternship.SharedAbstractions.Hooks.BunkbedReviveHooks;
using Unity.Netcode;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region BunkbedMod RPC

        [ServerRpc(RequireOwnership = false)]
        public void UpdateReviveCountServerRpc(int id)
        {
            UpdateReviveCountClientRpc(id);
        }

        [ClientRpc]
        private void UpdateReviveCountClientRpc(int id)
        {
            BunkbedReviveHook.UpdateReviveCount?.Invoke(id);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncGroupCreditsForNotOwnerTerminalServerRpc(int newGroupCredits, int numItemsInShip)
        {
            Terminal terminalScript = TerminalManager.Instance.GetTerminal();
            terminalScript.SyncGroupCreditsServerRpc(newGroupCredits, numItemsInShip);
        }

        #endregion
    }
}

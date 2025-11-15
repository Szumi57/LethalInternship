using LethalInternship.SharedAbstractions.Hooks.ReviveCompanyHooks;
using Unity.Netcode;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region ReviveCompany mod RPC

        [ServerRpc(RequireOwnership = false)]
        public void UpdateReviveCompanyRemainingRevivesServerRpc(string identityName)
        {
            UpdateReviveCompanyRemainingRevivesClientRpc(identityName);
        }

        [ClientRpc]
        private void UpdateReviveCompanyRemainingRevivesClientRpc(string identityName)
        {
            ReviveCompanyHook.UpdateReviveCompanyRemainingRevives?.Invoke(identityName);
        }

        #endregion
    }
}

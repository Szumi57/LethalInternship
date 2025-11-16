using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Linq;
using Unity.Netcode;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        private ClientRpcParams ClientRpcParams = new ClientRpcParams();

        #region Config RPC

        [ServerRpc(RequireOwnership = false)]
        public void SyncLoadedJsonIdentitiesServerRpc(ulong clientId)
        {
            PluginLoggerHook.LogDebug?.Invoke($"Client {clientId} ask server/host {NetworkManager.LocalClientId} to SyncLoadedJsonIdentities");
            ClientRpcParams.Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientId }
            };

            SyncLoadedJsonIdentitiesClientRpc(
                new ConfigIdentitiesNetworkSerializable()
                {
                    ConfigIdentities = PluginRuntimeProvider.Context.Config.ConfigIdentities.configIdentities.ToArray()
                },
                ClientRpcParams);
        }

        [ClientRpc]
        private void SyncLoadedJsonIdentitiesClientRpc(ConfigIdentitiesNetworkSerializable configIdentityNetworkSerializable,
                                                       ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
            {
                return;
            }

            PluginLoggerHook.LogInfo?.Invoke($"Client {NetworkManager.LocalClientId} : sync json interns identities");
            PluginLoggerHook.LogDebug?.Invoke($"Loaded {configIdentityNetworkSerializable.ConfigIdentities.Length} identities from server");
            foreach (ConfigIdentity configIdentity in configIdentityNetworkSerializable.ConfigIdentities)
            {
                PluginLoggerHook.LogDebug?.Invoke($"{configIdentity.ToString()}");
            }

            PluginLoggerHook.LogDebug?.Invoke($"Recreate identities for {configIdentityNetworkSerializable.ConfigIdentities.Length} interns");
            IdentityManager.Instance.InitIdentities(configIdentityNetworkSerializable.ConfigIdentities.ToArray());
        }

        #endregion
    }
}

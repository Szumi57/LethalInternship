using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        #region GiantKiwi compat patches

        [ServerRpc(RequireOwnership = false)]
        public void SyncWatchingThreatGiantKiwiServerRpc(NetworkObjectReference giantKiwiNOR)
        {
            SyncWatchingThreatGiantKiwiClientRpc(giantKiwiNOR);
        }

        [ClientRpc]
        private void SyncWatchingThreatGiantKiwiClientRpc(NetworkObjectReference giantKiwiNOR)
        {
            giantKiwiNOR.TryGet(out NetworkObject giantKiwiNO);
            GiantKiwiAI? giantKiwiAI = giantKiwiNO.gameObject.GetComponent<GiantKiwiAI>();
            if (giantKiwiAI == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncWatchingThreatGiantKiwiClientRpc intern {Npc.playerClientId} giantKiwiNOR -> giantKiwiAI null");
                return;
            }

            Type typeGiantKiwiAI = giantKiwiAI.GetType();
            IVisibleThreat? watchingThreat = this.npcController.Npc.GetComponent<IVisibleThreat>();
            if (giantKiwiAI == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncWatchingThreatGiantKiwiClientRpc intern {Npc.playerClientId} no IVisibleThreat");
                return;
            }

            typeGiantKiwiAI.GetField("watchingThreat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(giantKiwiAI, watchingThreat);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncAttackingThreatGiantKiwiServerRpc(NetworkObjectReference giantKiwiNOR)
        {
            SyncAttackingThreatGiantKiwiClientRpc(giantKiwiNOR);
        }

        [ClientRpc]
        private void SyncAttackingThreatGiantKiwiClientRpc(NetworkObjectReference giantKiwiNOR)
        {
            giantKiwiNOR.TryGet(out NetworkObject giantKiwiNO);
            GiantKiwiAI? giantKiwiAI = giantKiwiNO.gameObject.GetComponent<GiantKiwiAI>();
            if (giantKiwiAI == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncAttackingThreatGiantKiwiClientRpc intern {Npc.playerClientId} giantKiwiNOR -> giantKiwiAI null");
                return;
            }

            Type typeGiantKiwiAI = giantKiwiAI.GetType();
            IVisibleThreat? attackingThreat = this.npcController.Npc.GetComponent<IVisibleThreat>();
            if (giantKiwiAI == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncAttackingThreatGiantKiwiClientRpc intern {Npc.playerClientId} no IVisibleThreat");
                return;
            }

            typeGiantKiwiAI.GetField("watchingThreat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(giantKiwiAI, attackingThreat);
            typeGiantKiwiAI.GetField("attackingThreat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(giantKiwiAI, attackingThreat);

            giantKiwiAI.Screech(enraged: true);
            giantKiwiAI.SwitchToBehaviourStateOnLocalClient(stateIndex: 2);
        }

        #endregion

        #region Radmech compat patches

        [ServerRpc(RequireOwnership = false)]
        public void SyncSetTargetToThreatServerRpc(NetworkObjectReference radMechNOR, Vector3 lastSeenPos)
        {
            SyncSetTargetToThreatClientRpc(radMechNOR, lastSeenPos);
        }

        [ClientRpc]
        private void SyncSetTargetToThreatClientRpc(NetworkObjectReference radMechNOR, Vector3 lastSeenPos)
        {
            radMechNOR.TryGet(out NetworkObject radMechNO);
            RadMechAI? radMechAI = radMechNO.gameObject.GetComponent<RadMechAI>();
            if (radMechAI == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncSetTargetToThreatClientRpc intern {Npc.playerClientId} radMechNOR -> RadMechAI null");
                return;
            }

            if (!this.Npc.TryGetComponent<IVisibleThreat>(out IVisibleThreat visibleThreat))
            {
                PluginLoggerHook.LogError?.Invoke("Error: no IVisibleThreat on intern (SyncSetTargetToThreatClientRpc)");
                return;
            }

            radMechAI.focusedThreatTransform = visibleThreat.GetThreatTransform();
            float dist = Vector3.Distance(radMechAI.eye.position, radMechAI.focusedThreatTransform.position);
            radMechAI.SetTargetedThreat(visibleThreat, lastSeenPos, dist);
            radMechAI.SwitchToBehaviourStateOnLocalClient(stateIndex: 1);
        }

        #endregion
    }
}

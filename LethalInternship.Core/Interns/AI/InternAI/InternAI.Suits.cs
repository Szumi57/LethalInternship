using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        #region Interns suits

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSuitInternServerRpc(ulong idInternController, int suitID)
        {
            ChangeSuitIntern(idInternController, suitID, playAudio: true);
            ChangeSuitInternClientRpc(idInternController, suitID);
        }

        [ClientRpc]
        private void ChangeSuitInternClientRpc(ulong idInternController, int suitID)
        {
            if (IsServer)
            {
                return;
            }

            ChangeSuitIntern(idInternController, suitID, playAudio: true);
        }

        public void ChangeSuitIntern(ulong idInternController, int suitID, bool playAudio = false)
        {
            if (suitID > StartOfRound.Instance.unlockablesList.unlockables.Count())
            {
                suitID = 0;
            }

            PlayerControllerB internController = StartOfRound.Instance.allPlayerScripts[idInternController];

            UnlockableSuit.SwitchSuitForPlayer(internController, suitID, playAudio);
            internController.thisPlayerModelArms.enabled = false;
            StartCoroutine(WaitSecondsForChangeSuitToApply());
            InternIdentity.SuitID = suitID;

            PluginLoggerHook.LogDebug?.Invoke($"Changed suit of intern {NpcController.Npc.playerUsername} to {suitID}: {StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName}");
        }

        public bool HasInternModelReplacementAPI()
        {
            return PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded ? ModelReplacementAPIHook.HasComponentModelReplacementAPI?.Invoke(NpcController.Npc.gameObject) ?? false : false;
        }

        private IEnumerator WaitSecondsForChangeSuitToApply()
        {
            yield return new WaitForSeconds(0.2f);

            NpcController.RefreshBillBoardPosition();

            IInternCullingBodyInfo? internCullingBodyInfo = InternManager.Instance.GetInternCullingBodyInfo(NpcController.Npc.gameObject);
            if (internCullingBodyInfo != null)
            {
                internCullingBodyInfo.HasModelReplacement = HasInternModelReplacementAPI();
            }

            yield break;
        }

        #endregion
    }
}

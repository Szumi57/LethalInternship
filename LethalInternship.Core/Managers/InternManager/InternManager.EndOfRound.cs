using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using Unity.Netcode;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region SyncEndOfRoundInterns

        /// <summary>
        /// Only for the owner of <c>InternManager</c>, call server and clients to count intern left alive to re-drop on next round
        /// </summary>
        public void SyncEndOfRoundInterns()
        {
            if (!base.IsOwner)
            {
                return;
            }

            if (base.IsServer)
            {
                foreach (InternAI internAI in AllInternAIs)
                {
                    if (internAI == null
                        || internAI.isEnemyDead
                        || internAI.NpcController.Npc.isPlayerDead
                        || !internAI.NpcController.Npc.isPlayerControlled)
                    {
                        continue;
                    }

                    // Save weapon before dropping items
                    GrabbableObject? heldWeapon = internAI.HeldItems.GetHeldWeapon();
                    if (heldWeapon != null)
                    {
                        int itemId = 0;
                        for (int i = 0; i < StartOfRound.Instance.allItemsList.itemsList.Count; i++)
                        {
                            if (StartOfRound.Instance.allItemsList.itemsList[i].itemName == heldWeapon.itemProperties.itemName)
                            {
                                itemId = i;
                                break;
                            }
                        }
                        internAI.InternIdentity.UpdateItemsInInventory(new int[] { itemId });
                    }
                    // Drop all items + weapon
                    internAI.DropAllItems(EnumOptionsGetItems.All, waitBetweenItems: false);
                }

                SyncEndOfRoundInternsFromServerToClientRpc();
            }
            else
            {
                SyncEndOfRoundInternsFromClientToServerRpc();
            }
        }

        /// <summary>
        /// Server side, call clients to count intern left alive to re-drop on next round
        /// </summary>
        [ServerRpc]
        private void SyncEndOfRoundInternsFromClientToServerRpc()
        {
            SyncEndOfRoundInternsFromServerToClientRpc();
        }

        /// <summary>
        /// Client side, count intern left alive to re-drop on next round
        /// </summary>
        [ClientRpc]
        private void SyncEndOfRoundInternsFromServerToClientRpc()
        {
            EndOfRoundForInterns();
        }

        private void EndOfRoundForInterns()
        {
            DictEnemyAINoiseListeners.Clear();
            ListEnemyAINonNoiseListeners.Clear();

            CountAliveAndDisableInterns();
        }

        /// <summary>
        /// Count and disable the interns still alive
        /// </summary>
        /// <returns>Number of interns still alive</returns>
        private void CountAliveAndDisableInterns()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            if (instanceSOR.currentLevel.levelID == 3)
            {
                return;
            }

            PlayerControllerB internController;
            foreach (InternAI internAI in AllInternAIs)
            {
                if (internAI == null
                    || internAI.NpcController == null)
                {
                    continue;
                }

                internController = internAI.NpcController.Npc;

                DisableInternControllerModel(internController.gameObject, internController, enable: false, disableLocalArms: false);
                if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
                {
                    ModelReplacementAPIHook.RemoveInternModelReplacement?.Invoke(internAI, forceRemove: false);
                }

                if (internController.isPlayerDead
                    || !internController.isPlayerControlled)
                {
                    continue;
                }

                internController.isPlayerControlled = false;
                internController.localVisor.position = internController.playersManager.notSpawnedPosition.position;
                internController.transform.position = internController.playersManager.notSpawnedPosition.position;

                internAI.InternIdentity.Status = EnumStatusIdentity.ToDrop;
                instanceSOR.allPlayerObjects[internController.playerClientId].SetActive(false);
            }

            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.CleanListBodyReplacementOnDeadBodies?.Invoke();
            }

            if (HeldInternsLocalPlayer != null)
            {
                HeldInternsLocalPlayer.Clear();
            }
        }

        #endregion
    }
}

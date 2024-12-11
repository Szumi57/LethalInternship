using BunkbedRevive;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using LethalInternship.Patches.GameEnginePatches;
using System;

namespace LethalInternship.Patches.ModPatches.BunkbedRevive
{
    [HarmonyPatch(typeof(BunkbedController))]
    internal class BunkbedControllerPatch
    {
        [HarmonyPatch("OnInteract")]
        [HarmonyPrefix]
        static bool OnInteract_PreFix(InteractTrigger ___interactTrigger)
        {
            RagdollGrabbableObject? ragdollGrabbableObject = GetHeldBody_ReversePatch(BunkbedController.Instance);
            if (ragdollGrabbableObject == null)
            {
                return true;
            }

            int playerClientId = (int)ragdollGrabbableObject.ragdoll.playerScript.playerClientId;
            InternAI? internAI = InternManager.Instance.GetInternAI(playerClientId);
            if (internAI == null)
            {
                return true;
            }

            int reviveCost = BunkbedController.GetReviveCost();
            if (TerminalManager.Instance.GetTerminal().groupCredits < reviveCost)
            {
                HUDManagerPatch.DisplayGlobalNotification_ReversePatch(HUDManager.Instance, "Not enough credits");
                ___interactTrigger.StopInteraction();
                return false;
            }
            if (!BunkbedController.CanRevive(ragdollGrabbableObject.bodyID.Value, logStuff: true))
            {
                HUDManagerPatch.DisplayGlobalNotification_ReversePatch(HUDManager.Instance, "Can't Revive");
                ___interactTrigger.StopInteraction();
                return false;
            }
            Terminal terminalScript = TerminalManager.Instance.GetTerminal();
            terminalScript.groupCredits -= reviveCost;
            terminalScript.SyncGroupCreditsServerRpc(terminalScript.groupCredits, terminalScript.numberOfItemsInDropship);

            InternManager.Instance.SpawnThisInternServerRpc(internAI.InternIdentity.IdIdentity,
                                                            new NetworkSerializers.SpawnInternsParamsNetworkSerializable()
                                                            {
                                                                ShouldDestroyDeadBody = true,
                                                                SpawnPosition = StartOfRoundPatch.GetPlayerSpawnPosition_ReversePatch(StartOfRound.Instance, playerClientId, simpleTeleport: false),
                                                                YRot = 0,
                                                                IsOutside = true
                                                            });
            InternManager.Instance.UpdateReviveCountServerRpc(playerClientId + Plugin.PluginIrlPlayersCount);
            GameNetworkManager.Instance.localPlayerController?.DespawnHeldObject();

            HUDManagerPatch.DisplayGlobalNotification_ReversePatch(HUDManager.Instance, $"{internAI.NpcController.Npc.playerUsername} has been revived");
            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(InteractTrigger ___interactTrigger)
        {
            RagdollGrabbableObject? ragdollGrabbableObject = GetHeldBody_ReversePatch(BunkbedController.Instance);
            if (ragdollGrabbableObject == null)
            {
                return;
            }

            if (ragdollGrabbableObject.ragdoll != null
                && InternManager.Instance.IsPlayerIntern(ragdollGrabbableObject.ragdoll.playerScript))
            {
                ___interactTrigger.interactable = true;
            }
        }



        [HarmonyPatch("GetHeldBody")]
        [HarmonyReversePatch]
        public static RagdollGrabbableObject? GetHeldBody_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.BunkbedRevive.BunkbedControllerPatch.GetHeldBody_ReversePatch");

    }
}

using BunkbedRevive;
using HarmonyLib;
using LethalInternship.Patches.GameEnginePatches;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;

namespace LethalInternship.Patches.ModPatches.BunkbedRevive
{
    [HarmonyPatch(typeof(BunkbedController))]
    public class BunkbedControllerPatch
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
            string name = ragdollGrabbableObject.ragdoll.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText;
            IInternIdentity? internIdentity = IdentityManagerProvider.Instance.FindIdentityFromBodyName(name);
            if (internIdentity == null)
            {
                return true;
            }

            // Get the same logic as the mod at the beginning
            if (internIdentity.Alive)
            {
                PluginLoggerHook.LogError?.Invoke($"BunkbedRevive with LethalInternship: error when trying to revive intern \"{internIdentity.Name}\", intern is already alive! do nothing more");
                return false;
            }

            int reviveCost = BunkbedController.GetReviveCost();
            if (TerminalManagerProvider.Instance.GetTerminal().groupCredits < reviveCost)
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
            Terminal terminalScript = TerminalManagerProvider.Instance.GetTerminal();
            terminalScript.groupCredits -= reviveCost;
            InternManagerProvider.Instance.SyncGroupCreditsForNotOwnerTerminalServerRpc(terminalScript.groupCredits, terminalScript.numberOfItemsInDropship);

            InternManagerProvider.Instance.SpawnThisInternServerRpc(internIdentity.IdIdentity,
                                                                    new SpawnInternsParamsNetworkSerializable()
                                                                    {
                                                                        ShouldDestroyDeadBody = true,
                                                                        enumSpawnAnimation = (int)EnumSpawnAnimation.OnlyPlayerSpawnAnimation,
                                                                        SpawnPosition = StartOfRoundPatch.GetPlayerSpawnPosition_ReversePatch(StartOfRound.Instance, playerClientId, simpleTeleport: false),
                                                                        YRot = 0,
                                                                        IsOutside = true
                                                                    });
            InternManagerProvider.Instance.UpdateReviveCountServerRpc(internIdentity.IdIdentity + PluginRuntimeProvider.Context.PluginIrlPlayersCount);
            GameNetworkManager.Instance.localPlayerController?.DespawnHeldObject();

            HUDManagerPatch.DisplayGlobalNotification_ReversePatch(HUDManager.Instance, $"{internIdentity.Name} has been revived");
            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(InteractTrigger ___interactTrigger)
        {
            if (StartOfRound.Instance == null
                || GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            RagdollGrabbableObject? ragdollGrabbableObject = GetHeldBody_ReversePatch(BunkbedController.Instance);
            if (ragdollGrabbableObject == null)
            {
                return;
            }

            if (ragdollGrabbableObject.ragdoll != null
                && InternManagerProvider.Instance.IsPlayerIntern(ragdollGrabbableObject.ragdoll.playerScript))
            {
                ___interactTrigger.interactable = true;
            }
        }

        [HarmonyPatch("GetHeldBody")]
        [HarmonyReversePatch]
        public static RagdollGrabbableObject? GetHeldBody_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.ModPatches.BunkbedRevive.BunkbedControllerPatch.GetHeldBody_ReversePatch");

    }
}

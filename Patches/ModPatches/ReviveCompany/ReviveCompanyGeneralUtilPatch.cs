using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.Managers;
using OPJosMod.ReviveCompany;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ReviveCompany
{
    [HarmonyPatch(typeof(GeneralUtil))]
    internal class ReviveCompanyGeneralUtilPatch
    {
        [HarmonyPatch("RevivePlayer")]
        [HarmonyPrefix]
        static bool RevivePlayer_Prefix(int playerId)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI(playerId);
            if (internAI == null)
            {
                return true;
            }

            // Get the same logic as the mod at the beginning
            if (!internAI.isEnemyDead || !internAI.NpcController.Npc.isPlayerDead)
            {
                Plugin.LogError($"Revive company with LethalInternship: error when trying to revive intern {playerId} \"{internAI.NpcController.Npc.playerUsername}\", intern is already alive! do nothing more");
                return false;
            }

            GlobalVariables.RemainingRevives--;
            if (GlobalVariables.RemainingRevives < 100)
            {
                HUDManager.Instance.DisplayTip(internAI.NpcController.Npc.playerUsername + " was revived", string.Format("{0} revives remain!", GlobalVariables.RemainingRevives), false, false, "LC_Tip1");
            }

            Vector3 revivePos = internAI.NpcController.Npc.transform.position;
            float yRot = internAI.NpcController.Npc.transform.rotation.eulerAngles.y;
            bool isInsideFactory = false;
            if (internAI.NpcController.Npc.deadBody != null)
            {
                revivePos = internAI.NpcController.Npc.deadBody.transform.position;

                PlayerControllerB closestAlivePlayer = GeneralUtil.GetClosestAlivePlayer(internAI.NpcController.Npc.deadBody.transform.position);
                if (closestAlivePlayer != null)
                {
                    isInsideFactory = closestAlivePlayer.isInsideFactory;
                    if (Vector3.Distance(revivePos, closestAlivePlayer.transform.position) > 7f)
                    {
                        revivePos = closestAlivePlayer.transform.position;
                        yRot = closestAlivePlayer.transform.rotation.eulerAngles.y;
                    }
                }
            }

            // Respawn intern
            Plugin.LogDebug($"revive playerId {playerId}; intern {internAI.NpcController.Npc.playerClientId} {internAI.NpcController.Npc.playerUsername}");
            InternManager.Instance.SpawnThisInternServerRpc(internAI.InternIdentity.IdIdentity,
                                                            new NetworkSerializers.SpawnInternsParamsNetworkSerializable()
                                                            {
                                                                ShouldDestroyDeadBody = true,
                                                                enumSpawnAnimation = (int)EnumSpawnAnimation.OnlyPlayerSpawnAnimation,
                                                                SpawnPosition = revivePos,
                                                                YRot = yRot,
                                                                IsOutside = !isInsideFactory
                                                            });

            return false;
        }

        [HarmonyPatch("GetClosestDeadBody")]
        [HarmonyPostfix]
        static void GetClosestDeadBody_PostFix(ref RagdollGrabbableObject __result)
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            Ray interactRay = new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward);
            if (Physics.Raycast(interactRay, out RaycastHit hit, player.grabDistance, 1073742656) 
                && hit.collider.gameObject.layer != 8 && hit.collider.gameObject.layer != 30)
            {
                // Check if we are pointing to a ragdoll body of intern (not grabbable)
                if (hit.collider.tag == "PhysicsProp")
                {
                    RagdollGrabbableObject? ragdoll = hit.collider.gameObject.GetComponent<RagdollGrabbableObject>();
                    if (ragdoll == null)
                    {
                        return;
                    }

                    __result = ragdoll;
                }
            }

            if (__result != null && __result.ragdoll == null)
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                __result = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
    }
}

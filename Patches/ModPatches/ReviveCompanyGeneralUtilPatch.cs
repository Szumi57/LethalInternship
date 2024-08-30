using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using OPJosMod.ReviveCompany;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches
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

            GlobalVariables.RemainingRevives--;
            if (GlobalVariables.RemainingRevives < 100)
            {
                HUDManager.Instance.DisplayTip(internAI.NpcController.Npc.playerUsername + " was revived", string.Format("{0} revives remain!", GlobalVariables.RemainingRevives), false, false, "LC_Tip1");
            }

            // Respawn intern
            InternManager.Instance.SpawnThisInternServerRpc((int)internAI.NpcController.Npc.playerClientId, revivePos, yRot, !isInsideFactory);

            return false;
        }
    }
}

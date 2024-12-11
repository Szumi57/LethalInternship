using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using ModelReplacement;
using System;
using System.Linq;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ModelReplacementAPI))]
    internal class ModelReplacementAPIPatch
    {
        [HarmonyPatch("SetPlayerModelReplacement")]
        [HarmonyPrefix]
        static bool SetPlayerModelReplacement_Prefix(PlayerControllerB player, Type type)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                return true;
            }

            int currentSuitID = player.currentSuitID;
            string unlockableName = StartOfRound.Instance.unlockablesList.unlockables[currentSuitID].unlockableName;

            string suitNameToReplace = string.Empty;
            bool shouldAddNewBodyReplacement = true;
            BodyReplacementBase[] bodiesReplacementBase = internAI.ListModelReplacement.Select(x => (BodyReplacementBase)x).ToArray();
            //Plugin.LogDebug($"{player.playerUsername} SetPlayerModelReplacement bodiesReplacementBase.Length {bodiesReplacementBase.Length}");
            foreach (BodyReplacementBase bodyReplacementBase in bodiesReplacementBase)
            {
                if (BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Contains(bodyReplacementBase))
                {
                    continue;
                }

                if (bodyReplacementBase.GetType() == type
                    && bodyReplacementBase.suitName == unlockableName)
                {
                    shouldAddNewBodyReplacement = false;
                }
                else
                {
                    Plugin.LogInfo($"Patch LethalInternship, intern {player.playerUsername}, Model Replacement change detected {bodyReplacementBase.GetType()} => {type}, changing model.");
                    suitNameToReplace = bodyReplacementBase.suitName;
                    internAI.ListModelReplacement.Remove(bodyReplacementBase);
                    bodyReplacementBase.IsActive = false;
                    UnityEngine.Object.Destroy(bodyReplacementBase);
                    shouldAddNewBodyReplacement = true;
                }
            }

            if (shouldAddNewBodyReplacement && !internAI.NpcController.Npc.isPlayerDead)
            {
                Plugin.LogInfo($"Patch LethalInternship, intern {player.playerUsername}, Suit Change detected {suitNameToReplace} => {currentSuitID} {unlockableName}, Replacing {type}.");
                BodyReplacementBase bodyReplacementBaseToAdd = (BodyReplacementBase)player.gameObject.AddComponent(type);
                bodyReplacementBaseToAdd.suitName = unlockableName;
                internAI.ListModelReplacement.Add(bodyReplacementBaseToAdd);
            }

            return false;
        }

        [HarmonyPatch("RemovePlayerModelReplacement")]
        [HarmonyPrefix]
        static bool RemovePlayerModelReplacement_Prefix(PlayerControllerB player)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            BodyReplacementBase[] bodiesReplacementBase = internAI.ListModelReplacement.Select(x => (BodyReplacementBase)x).ToArray();
            //Plugin.LogDebug($"RemovePlayerModelReplacement bodiesReplacementBase.Length {bodiesReplacementBase.Length}");
            foreach (BodyReplacementBase bodyReplacementBase in bodiesReplacementBase)
            {
                if (BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Contains(bodyReplacementBase))
                {
                    continue;
                }

                internAI.ListModelReplacement.Remove(bodyReplacementBase);
                bodyReplacementBase.IsActive = false;
                UnityEngine.Object.Destroy(bodyReplacementBase);
            }

            return false;
        }
    }
}

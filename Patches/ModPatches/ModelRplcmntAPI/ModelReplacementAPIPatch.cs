using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using ModelReplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(ModelReplacementAPI))]
    public class ModelReplacementAPIPatch
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
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixOpenBodyCamTranspilerRemovePlayerModelReplacement_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString() == "call virtual void ModelReplacement.Monobehaviors.ManagerBase::ReportBodyReplacementRemoval()"
                    && codes[i + 1].ToString() == "call NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex + 1].opcode = OpCodes.Call;
                codes[startIndex + 1].operand = SymbolExtensions.GetMethodInfo(() => new ViewStateManager().UpdateModelReplacement());
                startIndex = -1;
            }
            else
            {
                Plugin.LogInfo($"LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementAPIPatch.FixOpenBodyCamTranspilerRemovePlayerModelReplacement_Transpiler, could not find call null line, ignoring fix.");
            }

            return codes.AsEnumerable();
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

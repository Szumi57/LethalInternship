using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
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
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                if ((int)player.playerClientId >= InternManagerProvider.Instance.IndexBeginOfInterns)
                {
                    // It's an intern but all intern have been disabled (ex : end of round)
                    // So ignore trying to set model
                    return false;
                }
                // Real player
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
            //PluginLoggerHook.LogDebug?.Invoke($"{player.playerUsername} SetPlayerModelReplacement bodiesReplacementBase.Length {bodiesReplacementBase.Length}");
            foreach (BodyReplacementBase bodyReplacementBase in bodiesReplacementBase)
            {
                if (BodyReplacementBasePatch.ListBodyReplacementOnDeadBodies.Contains(bodyReplacementBase))
                {
                    continue;
                }

                if (bodyReplacementBase.GetType() == type
                    && bodyReplacementBase.suitName == unlockableName)
                {
                    //PluginLoggerHook.LogDebug?.Invoke($"{player.playerUsername} shouldAddNewBodyReplacement false");
                    shouldAddNewBodyReplacement = false;
                }
                else
                {
                    PluginLoggerHook.LogInfo?.Invoke($"Patch LethalInternship, intern {player.playerUsername}, Model Replacement change detected {bodyReplacementBase.GetType()} => {type}, changing model.");
                    suitNameToReplace = bodyReplacementBase.suitName;
                    internAI.ListModelReplacement.Remove(bodyReplacementBase);
                    bodyReplacementBase.IsActive = false;
                    UnityEngine.Object.Destroy(bodyReplacementBase);
                    shouldAddNewBodyReplacement = true;
                }
            }

            //PluginLoggerHook.LogDebug?.Invoke($"{player.playerUsername} shouldAddNewBodyReplacement {shouldAddNewBodyReplacement}");
            if (shouldAddNewBodyReplacement
                && !internAI.NpcController.Npc.isPlayerDead
                && internAI.NpcController.Npc.isPlayerControlled)
            {
                PluginLoggerHook.LogInfo?.Invoke($"Patch LethalInternship, intern {player.playerUsername}, Suit Change detected {suitNameToReplace} => {currentSuitID} {unlockableName}, Replacing {type}.");
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
                PluginLoggerHook.LogInfo?.Invoke($"LethalInternship.Patches.ModPatches.ModelRplcmntAPI.ModelReplacementAPIPatch.FixOpenBodyCamTranspilerRemovePlayerModelReplacement_Transpiler, could not find call null line, ignoring fix.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("RemovePlayerModelReplacement")]
        [HarmonyPrefix]
        static bool RemovePlayerModelReplacement_Prefix(PlayerControllerB player)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            ModelReplacementAPIUtils.RemoveInternModelReplacement(internAI);
            return false;
        }
    }
}

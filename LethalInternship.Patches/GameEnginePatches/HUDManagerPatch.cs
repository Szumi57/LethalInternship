using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for the <c>HUDManager</c>
    /// </summary>
    [HarmonyPatch(typeof(HUDManager))]
    [HarmonyAfter(Const.BETTER_EXP_GUID)]
    public class HUDManagerPatch
    {
        [HarmonyPatch("FillEndGameStats")]
        [HarmonyPrefix]
        static void FillEndGameStats_PreFix(HUDManager __instance)
        {
            // Why hudmanager statsUIElements gets to length 0 on client ?
            if (InternManagerProvider.Instance.AllEntitiesCount == 0
                || __instance.statsUIElements.playerNamesText.Length < InternManagerProvider.Instance.AllEntitiesCount)
            {
                ResizeStatsUIElements(__instance);
            }
        }

        #region Reverse patches

        [HarmonyPatch("DisplayGlobalNotification")]
        [HarmonyReversePatch]
        public static void DisplayGlobalNotification_ReversePatch(object instance, string displayText) => throw new NotImplementedException("Stub LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.DisplayGlobalNotification_ReversePatch");

        #endregion

        #region Transpilers

        /// <summary>
        /// Patch for making the hud only show end games stats for irl players, not interns
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("FillEndGameStats")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FillEndGameStats_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //170
                    && codes[i + 1].ToString() == "ldfld StartOfRound HUDManager::playersManager"
                    && codes[i + 2].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 3].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Nop;
                codes[startIndex + 2].operand = null;
                codes[startIndex + 3].opcode = OpCodes.Call;
                codes[startIndex + 3].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.FillEndGameStats_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("SyncAllPlayerLevelsServerRpc", new Type[] { })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SyncAllPlayerLevelsServerRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.SyncAllPlayerLevelsServerRpc_Transpiler 1 could not use irl number of player in list.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.SyncAllPlayerLevelsServerRpc_Transpiler 2 could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("SyncAllPlayerLevelsClientRpc", new Type[] { typeof(int[]), typeof(bool[]) })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SyncAllPlayerLevelsClientRpc_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.SyncAllPlayerLevelsClientRpc_Transpiler could not use irl number of player in list.");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("UpdateBoxesSpectateUI")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UpdateBoxesSpectateUI_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static StartOfRound StartOfRound::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB[] StartOfRound::allPlayerScripts"
                    && codes[i + 2].ToString() == "ldlen NULL")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Nop;
                codes[startIndex + 1].operand = null;
                codes[startIndex + 2].opcode = OpCodes.Call;
                codes[startIndex + 2].operand = PatchesUtil.IndexBeginOfInternsMethod;
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.GameEnginePatches.HUDManagerPatch.UpdateBoxesSpectateUI_Transpiler could not use irl number of player for iteration.");
            }

            return codes.AsEnumerable();
        }

        #endregion

        #region PostFixes

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(HUDManager __instance)
        {
            ResizeStatsUIElements(__instance);

            UIManagerProvider.Instance.InitUI(__instance.HUDContainer.transform.parent);
        }

        private static void ResizeStatsUIElements(HUDManager instance)
        {
            EndOfGameStatUIElements statsUIElements = instance.statsUIElements;
            GameObject gameObjectParent = statsUIElements.playerNamesText[0].gameObject.transform.parent.gameObject;

            int allEntitiesCount = InternManagerProvider.Instance.AllEntitiesCount == 0 ? PluginRuntimeProvider.Context.PluginIrlPlayersCount : InternManagerProvider.Instance.AllEntitiesCount;
            Array.Resize(ref statsUIElements.playerNamesText, allEntitiesCount);
            Array.Resize(ref statsUIElements.playerStates, allEntitiesCount);
            Array.Resize(ref statsUIElements.playerNotesText, allEntitiesCount);

            for (int i = InternManagerProvider.Instance.IndexBeginOfInterns; i < allEntitiesCount; i++)
            {
                GameObject newGameObjectParent = Object.Instantiate(gameObjectParent);
                GameObject gameObjectPlayerName = newGameObjectParent.transform.Find("PlayerName1").gameObject;
                GameObject gameObjectNotes = newGameObjectParent.transform.Find("Notes").gameObject;
                GameObject gameObjectSymbol = newGameObjectParent.transform.Find("Symbol").gameObject;

                statsUIElements.playerNamesText[i] = gameObjectPlayerName.GetComponent<TextMeshProUGUI>();
                statsUIElements.playerNotesText[i] = gameObjectNotes.GetComponent<TextMeshProUGUI>();
                statsUIElements.playerStates[i] = gameObjectSymbol.GetComponent<Image>();
            }

            PluginLoggerHook.LogDebug?.Invoke($"ResizeStatsUIElements {instance.statsUIElements.playerNamesText.Length}");
        }

        [HarmonyPatch("ChangeControlTipMultiple")]
        [HarmonyPostfix]
        public static void ChangeControlTipMultiple_Postfix(HUDManager __instance)
        {
            UIManagerProvider.Instance.AddInternsControlTip(__instance);
        }

        [HarmonyPatch("ClearControlTips")]
        [HarmonyPostfix]
        public static void ClearControlTips_Postfix(HUDManager __instance)
        {
            UIManagerProvider.Instance.AddInternsControlTip(__instance);
        }

        [HarmonyPatch("ChangeControlTip")]
        [HarmonyPostfix]
        public static void ChangeControlTip_Postfix(HUDManager __instance)
        {
            UIManagerProvider.Instance.AddInternsControlTip(__instance);
        }

        #endregion
    }
}

using HarmonyLib;
using LethalInternship.Managers;
using System;

namespace LethalInternship.Patches.TerminalPatches
{
    /// <summary>
    /// Patches for the <c>Terminal</c>
    /// </summary>
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        /// <summary>
        /// Patch add the text introducing the intern shop
        /// </summary>
        /// <param name="___terminalNodes"></param>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_Postfix(TerminalNodesList ___terminalNodes)
        {
            TerminalManager.Instance.AddTextToHelpTerminalNode(___terminalNodes);
        }

        /// <summary>
        /// Patch for parsing commands after original game code commands, for the intern shop
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        [HarmonyPatch("ParsePlayerSentence")]
        [HarmonyPostfix]
        static void ParsePlayerSentence_Postfix(ref Terminal __instance, ref TerminalNode __result)
        {
            string command = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);

            if (__result != null
                && __result != __instance.terminalNodes.specialNodes[10] // ParserError1 (TerminalNode)
                && __result != __instance.terminalNodes.specialNodes[11] // ParserError2 (TerminalNode)
                && __result != __instance.terminalNodes.specialNodes[12] // ParserError3 (TerminalNode)
                && command != Plugin.Config.GetTitleInternshipProgram()) 
            {
                // Command valid parsed by base game
                TerminalManager.Instance.ResetTerminalParser();
                return;
            }

            TerminalNode? lethalInternshipTerminalNode = TerminalManager.Instance.ParseLethalInternshipCommands(command, ref __instance);
            if (lethalInternshipTerminalNode != null)
            {
                __result = lethalInternshipTerminalNode;
            }
        }

        /// <summary>
        /// Reverse patch to call <c>ParseWord</c>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("ParseWord")]
        [HarmonyReversePatch]
        public static TerminalKeyword ParseWord_ReversePatch(object instance, string playerWord, int specificityRequired) => throw new NotImplementedException("Stub LethalInternship.Patches.TerminalPatches.ParseWord_ReversePatch");

        /// <summary>
        /// Reverse patch to call <c>ParseWordOverrideOptions</c>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("ParseWordOverrideOptions")]
        [HarmonyReversePatch]
        public static TerminalNode ParseWordOverrideOptions_ReversePatch(object instance, string playerWord, CompatibleNoun[] options) => throw new NotImplementedException("Stub LethalInternship.Patches.TerminalPatches.ParseWordOverrideOptions_ReversePatch");

        /// <summary>
        /// Reverse patch to call <c>RemovePunctuation</c>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("RemovePunctuation")]
        [HarmonyReversePatch]
        public static string RemovePunctuation_ReversePatch(object instance, string s) => throw new NotImplementedException("Stub LethalInternship.Patches.TerminalPatches.RemovePunctuation_ReversePatch");

    }
}

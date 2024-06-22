using HarmonyLib;
using LethalInternship.Managers;
using System;

namespace LethalInternship.Patches.TerminalPatches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_Postfix(TerminalNodesList ___terminalNodes)
        {
            TerminalManager.Instance.AddTextToHelpTerminalNode(___terminalNodes);
        }

        [HarmonyPatch("ParsePlayerSentence")]
        [HarmonyPostfix]
        static void ParsePlayerSentence_Postfix(ref Terminal __instance, ref TerminalNode __result)
        {
            string command = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            TerminalNode? lethalInternshipTerminalNode = TerminalManager.Instance.ParseLethalInternshipCommands(command, ref __instance);
            if(lethalInternshipTerminalNode != null)
            {
                __result = lethalInternshipTerminalNode;
            }
        }

        [HarmonyPatch("ParseWord")]
        [HarmonyReversePatch]
        public static TerminalKeyword ParseWord_ReversePatch(object instance, string playerWord, int specificityRequired) => throw new NotImplementedException("Stub LethalInternship.Patches.TerminalPatches.ParseWord_ReversePatch");

        [HarmonyPatch("ParseWordOverrideOptions")]
        [HarmonyReversePatch]
        public static TerminalNode ParseWordOverrideOptions_ReversePatch(object instance, string playerWord, CompatibleNoun[] options) => throw new NotImplementedException("Stub LethalInternship.Patches.TerminalPatches.ParseWordOverrideOptions_ReversePatch");
        
        [HarmonyPatch("RemovePunctuation")]
        [HarmonyReversePatch]
        public static string RemovePunctuation_ReversePatch(object instance, string s) => throw new NotImplementedException("Stub LethalInternship.Patches.TerminalPatches.RemovePunctuation_ReversePatch");

    }
}

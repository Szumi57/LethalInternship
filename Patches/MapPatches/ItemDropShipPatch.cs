using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(ItemDropship))]
    internal class ItemDropShipPatch
    {
        static MethodInfo AreInternsScheduledToLandMethod = SymbolExtensions.GetMethodInfo(() => InternManager.AreInternsScheduledToLand());

        [HarmonyPatch("LandShipOnServer")]
        [HarmonyReversePatch]
        public static void LandShipOnServer_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.MapPatches.LandShipOnServer_ReversePatch");

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // do not count living players down if is intern
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL"//6
                    && codes[i + 1].ToString() == "ldfld Terminal ItemDropship::terminalScript"
                    && codes[i + 2].ToString() == "ldfld System.Collections.Generic.List<int> Terminal::orderedItemsFromTerminal"
                    && codes[i + 6].ToString() == "ldarg.0 NULL" //12
                    && codes[i + 8].ToString() == "ldfld bool StartOfRound::shipHasLanded")//14
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<Label> labelsOfCodeToJumpTo = codes[startIndex + 6].labels;

                // Define label for the jump
                Label labelToJumpTo = generator.DefineLabel();
                labelsOfCodeToJumpTo.Add(labelToJumpTo);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                                                        {
                                                            new CodeInstruction(OpCodes.Call, AreInternsScheduledToLandMethod),
                                                            new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
                                                        };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.MapPatches.ItemDropShipPatch.Update_Transpiler could not bypass condition if interns have to land");
            }

            // ----------------------------------------------------------------------
            return codes.AsEnumerable();
        }

        [HarmonyPatch("OpenShipDoorsOnServer")]
        [HarmonyPostfix]
        static void OpenShipDoorsOnServer_PostFix(ref ItemDropship __instance)
        {
            if (!InternManager.AreInternsScheduledToLand())
            {
                return;
            }

            if (!__instance.shipLanded)
            {
                return;
            }

            InternManager.Instance.SpawnInternsFromDropShip(__instance.itemSpawnPositions);
        }
    }
}

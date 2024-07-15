using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.MapPatches
{
    /// <summary>
    /// Patch for the <c>ItemDropship</c>
    /// </summary>
    [HarmonyPatch(typeof(ItemDropship))]
    internal class ItemDropShipPatch
    {
        /// <summary>
        /// Patch for making the item drop ship check if interns are scheduled to land, to know if it should spawn
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 13; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL")// 45
                    && codes[i + 1].ToString().StartsWith("ldfld Terminal ItemDropship::terminalScript")
                    && codes[i + 2].ToString().StartsWith("ldfld System.Collections.Generic.List<int> Terminal::orderedItemsFromTerminal")
                    && codes[i + 11].ToString().StartsWith("ldarg.0 NULL") // 56
                    && codes[i + 13].ToString().StartsWith("ldfld bool StartOfRound::shipHasLanded"))// 58
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                List<Label> labelsOfCodeToJumpTo = codes[startIndex + 11].labels;

                // Define label for the jump
                Label labelToJumpTo = generator.DefineLabel();
                labelsOfCodeToJumpTo.Add(labelToJumpTo);

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                                                        {
                                                            new CodeInstruction(OpCodes.Call, PatchesUtil.AreInternsScheduledToLandMethod),
                                                            new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
                                                        };
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.MapPatches.ItemDropShipPatch.Update_Transpiler could not bypass condition if interns have to land");
            }

            // ----------------------------------------------------------------------
            return codes.AsEnumerable();
        }

        /// <summary>
        /// Patch for spawning the interns ordered when opening the doors or the drop ship
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("OpenShipDoorsOnServer")]
        [HarmonyPostfix]
        static void OpenShipDoorsOnServer_PostFix(ref ItemDropship __instance)
        {
            if (!InternManager.Instance.AreInternsScheduledToLand())
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

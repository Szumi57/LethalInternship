using HarmonyLib;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(SpringManAI))]
    internal class SpringManAIPatch
    {
        private static FieldInfo fieldInfoAllEntitiesCount = AccessTools.Field(typeof(InternManager), "AllEntitiesCount");

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            Plugin.Logger.LogDebug($"Update ======================");
            for (var i = 0; i < codes.Count; i++)
            {
                Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            }
            Plugin.Logger.LogDebug($"Update ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "ldc.i4.4 NULL" || codes[i].ToString() == "ldsfld int MoreCompany.MainClass::newPlayerCount")//110
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Ldsfld;
                codes[startIndex].operand = fieldInfoAllEntitiesCount;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.SpringManAIPatch.Update_Transpiler could not change size of player array to look up.");
            }

            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"Update ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"Update ======================");
            return codes.AsEnumerable();
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DoAIInterval_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"DoAIInterval ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DoAIInterval ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].ToString() == "ldc.i4.4 NULL" || codes[i].ToString() == "ldsfld int MoreCompany.MainClass::newPlayerCount")//81
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Ldsfld;
                codes[startIndex].operand = fieldInfoAllEntitiesCount;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.SpringManAIPatch.Update_Transpiler could not change size of player array to look up.");
            }

            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"DoAIInterval ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"DoAIInterval ======================");
            return codes.AsEnumerable();
        }
    }
}

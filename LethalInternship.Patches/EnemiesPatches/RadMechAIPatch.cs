using HarmonyLib;
using LethalInternship.Patches.Utils;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(RadMechAI))]
    public class RadMechAIPatch
    {
        [HarmonyPatch("CheckSightForThreat")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CheckSightForThreat_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 14; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") // 190
                    && codes[i + 14].ToString().StartsWith("call void RadMechAI::SetTargetToThreatClientRpc")) // 204
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // startIndex = 190
                Label labelToJumpTo = generator.DefineLabel();
                codes[startIndex + 15].labels.Add(labelToJumpTo); // 205

                List<CodeInstruction> codesToAdd = new List<CodeInstruction>
                {
                    // RadMechAI 
                    new CodeInstruction(codes[startIndex - 4]),  // ldarg.0 NULL // 186

                    // IVisibleThreat intern
                    new CodeInstruction(codes[startIndex - 3]),  // ldloc.3 NULL // 187
                    
                    // lastSeenPosition
                    new CodeInstruction(codes[startIndex + 11]), // ldarg.0 NULL // 201
                    new CodeInstruction(codes[startIndex + 12]), // ldfld Threat RadMechAI::targetedThreat // 202
                    new CodeInstruction(codes[startIndex + 13]), // ldfld UnityEngine.Vector3 Threat::lastSeenPosition // 203

                    new CodeInstruction(OpCodes.Call, PatchesUtil.SyncSetTargetToThreatIfInternMethod),
                    new CodeInstruction(OpCodes.Brtrue, labelToJumpTo),
                };

                //-----------------------------
                codes.InsertRange(startIndex, codesToAdd);
                startIndex = -1;
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"LethalInternship.Patches.EnemiesPatches.RadMechAIPatch.CheckSightForThreat_Transpiler could not bypass network object method call with intern");
            }

            return codes.AsEnumerable();
        }
    }
}

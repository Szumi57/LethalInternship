﻿using HarmonyLib;
using LethalInternship.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalInternship.Patches.MapHazardsPatches
{
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    internal class SpikeRoofTrapPatch
    {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnTriggerStay_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"OnTriggerStay ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"OnTriggerStay ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //24
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"//25
                    && codes[i + 2].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")//26
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;

                codes[startIndex + 1].opcode = OpCodes.Call;
                codes[startIndex + 1].operand = PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod;

                codes[startIndex + 2].opcode = OpCodes.Nop; //op_Equality
                codes[startIndex + 2].operand = null;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.MapHazardsPatches.SpikeRoofTrapPatch.OnTriggerStay_Transpiler could not change check for local player or intern.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 8; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //31
                    && codes[i + 8].ToString() == "callvirt void GameNetcodeStuff.PlayerControllerB::KillPlayer(UnityEngine.Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation)")//39
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;

                codes[startIndex + 1].opcode = OpCodes.Ldloc_0;
                codes[startIndex + 1].operand = null;

                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.MapHazardsPatches.SpikeRoofTrapPatch.OnTriggerStay_Transpiler could not change use of component for method kill player.");
            }

            // ----------------------------------------------------------------------
            //Plugin.Logger.LogDebug($"OnTriggerStay ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"OnTriggerStay ======================");
            return codes.AsEnumerable();
        }

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            //Plugin.Logger.LogDebug($"Update ======================");
            //for (var i = 0; i < codes.Count; i++)
            //{
            //    Plugin.Logger.LogDebug($"{i} {codes[i].ToString()}");
            //}
            //Plugin.Logger.LogDebug($"Update ======================");

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //98
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 2].ToString() == "call static bool UnityEngine.Object::op_Equality(UnityEngine.Object x, UnityEngine.Object y)")
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes[startIndex].operand = null;

                codes[startIndex + 1].opcode = OpCodes.Call;
                codes[startIndex + 1].operand = PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod;

                codes[startIndex + 2].opcode = OpCodes.Nop; //op_Equality
                codes[startIndex + 2].operand = null;

                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.MapHazardsPatches.SpikeRoofTrapPatch.Update_Transpiler could not change check for local player or intern.");
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
    }
}

using HarmonyLib;
using LethalInternship.Managers;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(ForestGiantAI))]
    internal class ForestGiantAIPatch
    {
        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnCollideWithPlayer_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);

            
            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 18; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()" //31
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController"
                    && codes[i + 18].ToString() == "call static UnityEngine.Vector3 UnityEngine.Vector3::Normalize(UnityEngine.Vector3 value)") //49
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
                codes[startIndex + 2].operand = PatchesUtil.IsPlayerLocalOrInternOwnerLocalMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.Logger.LogError($"LethalInternship.Patches.EnemiesPatches.ForestGiantAIPatch.OnCollideWithPlayer_Transpiler could not check if player local or intern");
            }

            // ----------------------------------------------------------------------
            // Replace on all occurences localPlayerController by the player from getComponent just before, so the player is local or intern
            for (var i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].ToString() == "call static GameNetworkManager GameNetworkManager::get_Instance()"
                    && codes[i + 1].ToString() == "ldfld GameNetcodeStuff.PlayerControllerB GameNetworkManager::localPlayerController")
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                    codes[i + 1].opcode = OpCodes.Ldloc_0;
                    codes[i + 1].operand = null;
                }
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch("LookForPlayers")]
        [HarmonyPrefix]
        [HarmonyAfter(Const.MORECOMPANY_GUID)]
        static void LookForPlayers_Prefix(ref ForestGiantAI __instance)
        {
            InternManager manager = InternManager.Instance;
            if (__instance.playerStealthMeters.Length != manager.AllEntitiesCount)
            {
                Array.Resize(ref __instance.playerStealthMeters, manager.AllEntitiesCount);
                for (int i = 0; i < manager.AllEntitiesCount; i++)
                {
                    __instance.playerStealthMeters[i] = 0f;
                }
            }
        }
    }
}

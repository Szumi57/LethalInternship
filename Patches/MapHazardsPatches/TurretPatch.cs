using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LethalInternship.Patches.MapHazardsPatches
{
    /// <summary>
    /// Patch for the <c>Turret</c>
    /// </summary>
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        static MethodInfo DamagePlayersInLOSMethod = SymbolExtensions.GetMethodInfo(() => TurretPatch.DamagePlayersInLOS(new Turret()));

        /// <summary>
        /// Patch for making the turret able to detect intern and kill them by using another methode
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
            for (var i = 0; i < codes.Count - 39; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //306
                    && codes[i + 3].ToString().StartsWith("call GameNetcodeStuff.PlayerControllerB Turret::CheckForPlayersInLineOfSight(")//309
                    && codes[i + 39].ToString().StartsWith("callvirt void GameNetcodeStuff.PlayerControllerB::KillPlayer("))//345
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                for (var i = startIndex; i < startIndex + 4; i++)
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }

                codes[startIndex].opcode = OpCodes.Ldarg_0;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Call;
                codes[startIndex + 1].operand = DamagePlayersInLOSMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.MapHazardsPatches.TurretPatch.Update_Transpiler use other method for shooting player/intern in TurretMode.Firing");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 39; i++)
            {
                if (codes[i].ToString().StartsWith("ldarg.0 NULL") //490
                    && codes[i + 3].ToString().StartsWith("call GameNetcodeStuff.PlayerControllerB Turret::CheckForPlayersInLineOfSight(")//493
                    && codes[i + 39].ToString().StartsWith("callvirt void GameNetcodeStuff.PlayerControllerB::KillPlayer("))//529
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                for (var i = startIndex; i < startIndex + 4; i++)
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }

                codes[startIndex].opcode = OpCodes.Ldarg_0;
                codes[startIndex].operand = null;
                codes[startIndex + 1].opcode = OpCodes.Call;
                codes[startIndex + 1].operand = DamagePlayersInLOSMethod;
                startIndex = -1;
            }
            else
            {
                Plugin.LogError($"LethalInternship.Patches.MapHazardsPatches.TurretPatch.Update_Transpiler use other method for shooting player/intern in TurretMode.berzerk");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Method injected in code, for checking intern and damage/kill them
        /// </summary>
        /// <param name="turret"></param>
        private static PlayerControllerB? DamagePlayersInLOS(Turret turret)
        {
            PlayerControllerB player = turret.CheckForPlayersInLineOfSight(3f, false);
            if (player == null)
            {
                return null;
            }

            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)player.playerClientId);
            if (internAI == null)
            {
                // Player not intern
                return player;
            }

            // intern
            if (player.health > 50)
            {
                player.DamagePlayer(50, hasDamageSFX: false, callRPC: false, CauseOfDeath.Gunshots, 0, false, default);
            }
            else
            {
                Plugin.LogDebug($"SyncKillIntern from turret for LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.NpcController.Npc.KillPlayer(turret.aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots, 0, default);
            }

            return null;
        }
    }
}

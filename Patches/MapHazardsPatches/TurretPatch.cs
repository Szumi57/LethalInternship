using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace LethalInternship.Patches.MapHazardsPatches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        static MethodInfo DamagePlayersInLOSMethod = SymbolExtensions.GetMethodInfo(() => TurretPatch.DamagePlayersInLOS(new Turret()));

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
            for (var i = 0; i < codes.Count - 36; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //306
                    && codes[i + 3].ToString() == "call GameNetcodeStuff.PlayerControllerB Turret::CheckForPlayersInLineOfSight(float radius, bool angleRangeCheck)"//309
                    && codes[i + 36].ToString() == "callvirt void GameNetcodeStuff.PlayerControllerB::KillPlayer(UnityEngine.Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation)")//342
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                for (var i = startIndex; i < startIndex + 37; i++)
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
                Plugin.Logger.LogError($"LethalInternship.Patches.MapHazardsPatches.TurretPatch.Update_Transpiler use other method for shooting player/intern normal mode.");
            }

            // ----------------------------------------------------------------------
            for (var i = 0; i < codes.Count - 36; i++)
            {
                if (codes[i].ToString() == "ldarg.0 NULL" //487
                    && codes[i + 3].ToString() == "call GameNetcodeStuff.PlayerControllerB Turret::CheckForPlayersInLineOfSight(float radius, bool angleRangeCheck)"//490
                    && codes[i + 36].ToString() == "callvirt void GameNetcodeStuff.PlayerControllerB::KillPlayer(UnityEngine.Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation)")//523
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                for (var i = startIndex; i < startIndex + 37; i++)
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
                Plugin.Logger.LogError($"LethalInternship.Patches.MapHazardsPatches.TurretPatch.Update_Transpiler use other method for shooting player/intern berzerk mode.");
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

        public static void DamagePlayersInLOS(Turret turret)
        {
            PlayerControllerB player = turret.CheckForPlayersInLineOfSight(3f, false);
            if (PatchesUtil.IsPlayerLocalOrInternOwnerLocal(player))
            {
                if (player.health > 50)
                {
                    player.DamagePlayer(50, true, true, CauseOfDeath.Gunshots, 0, false, default(Vector3));
                }
                else
                {
                    player.KillPlayer(turret.aimPoint.forward * 40f, true, CauseOfDeath.Gunshots, 0);
                }
            }
        }
    }
}

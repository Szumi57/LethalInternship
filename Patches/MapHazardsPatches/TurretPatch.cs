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
                Plugin.LogError($"LethalInternship.Patches.MapHazardsPatches.TurretPatch.Update_Transpiler use other method for shooting player/intern normal mode.");
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
                Plugin.LogError($"LethalInternship.Patches.MapHazardsPatches.TurretPatch.Update_Transpiler use other method for shooting player/intern berzerk mode.");
            }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Method injected in code, for checking intern and damage/kill them
        /// </summary>
        /// <param name="turret"></param>
        private static void DamagePlayersInLOS(Turret turret)
        {
            PlayerControllerB player = turret.CheckForPlayersInLineOfSight(3f, false);
            if (player == null)
            {
                return;
            }

            InternAI? internAI = InternManager.Instance.GetInternAIIfLocalIsOwner((int)player.playerClientId);
            if (internAI == null)
            {
                return;
            }

            if (player.health > 50)
            {
                internAI.SyncDamageIntern(50, CauseOfDeath.Gunshots, 0, false, default);
            }
            else
            {
                Plugin.LogDebug($"SyncKillIntern from turret for LOCAL client #{internAI.NetworkManager.LocalClientId}, intern object: Intern #{internAI.InternId}");
                internAI.SyncKillIntern(turret.aimPoint.forward * 40f, true, CauseOfDeath.Gunshots, 0);
            }
        }
    }
}

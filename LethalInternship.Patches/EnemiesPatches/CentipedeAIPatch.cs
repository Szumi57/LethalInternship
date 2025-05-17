using HarmonyLib;
using LethalInternship.SharedAbstractions.ManagerProviders;
using System;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patches for <c>CentipedeAI</c>
    /// </summary>
    [HarmonyPatch(typeof(CentipedeAI))]
    public class CentipedeAIPatch
    {
        /// <summary>
        /// Patch for making the centipede hurt the intern
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_PostFix(ref CentipedeAI __instance)
        {
            if (__instance.isEnemyDead)
            {
                return;
            }

            switch (__instance.currentBehaviourStateIndex)
            {
                case 3:
                    if (__instance.clingingToPlayer == null)
                    {
                        break;
                    }

                    if (InternManagerProvider.Instance.IsPlayerInternOwnerLocal(__instance.clingingToPlayer))
                    {
                        DamagePlayerOnIntervals_ReversePatch(__instance);
                    }
                    break;
            }
        }

        /// <summary>
        /// Reverse patch used for damaging intern
        /// </summary>
        /// <param name="instance"></param>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyPatch("DamagePlayerOnIntervals")]
        [HarmonyReversePatch]
        public static void DamagePlayerOnIntervals_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.EnemiesPatches.DamagePlayerOnIntervals");

    }
}

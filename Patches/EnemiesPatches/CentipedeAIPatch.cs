using HarmonyLib;
using LethalInternship.Utils;
using System;

namespace LethalInternship.Patches.EnemiesPatches
{
    [HarmonyPatch(typeof(CentipedeAI))]
    internal class CentipedeAIPatch
    {
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

                    if (PatchesUtil.IsPlayerIntern(__instance.clingingToPlayer))
                    {
                        DamagePlayerOnIntervals_ReversePatch(__instance);
                    }
                    break;
            }
        }

        [HarmonyPatch("DamagePlayerOnIntervals")]
        [HarmonyReversePatch]
        public static void DamagePlayerOnIntervals_ReversePatch(object instance) => throw new NotImplementedException("Stub LethalInternship.Patches.EnemiesPatches.DamagePlayerOnIntervals");

    }
}

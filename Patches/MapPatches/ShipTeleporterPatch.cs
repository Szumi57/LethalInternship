using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.Managers;
using System;
using Random = System.Random;

namespace LethalInternship.Patches.MapPatches
{
    [HarmonyPatch(typeof(ShipTeleporter))]
    internal class ShipTeleporterPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyAfter(Const.MORECOMPANY_GUID)]
        [HarmonyPostfix]
        public static void Awake_Postfix(ref ShipTeleporter __instance, ref int[] ___playersBeingTeleported)
        {
            int[] array = new int[InternManager.Instance.AllEntitiesCount];
            Array.Fill(array, -1);
            ___playersBeingTeleported = array;
        }

        [HarmonyPatch("beamOutPlayer")]
        [HarmonyPostfix]
        static void beamOutPlayer_PostFix(ShipTeleporter __instance,
                                          Random ___shipTeleporterSeed)
        {
            InternManager.Instance.TeleportOutInterns(__instance, ___shipTeleporterSeed);
        }

        [HarmonyPatch("SetPlayerTeleporterId")]
        [HarmonyReversePatch]
        public static void SetPlayerTeleporterId_ReversePatch(object instance, PlayerControllerB playerScript, int teleporterId) => throw new NotImplementedException("Stub LethalInternship.Patches.MapPatches.ShipTeleporterPatch.SetPlayerTeleporterId_ReversePatch");
    }
}

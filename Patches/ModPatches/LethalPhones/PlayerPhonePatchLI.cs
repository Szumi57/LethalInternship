using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using Scoops.misc;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.LethalPhones
{
    [HarmonyPatch(typeof(PlayerPhone))]
    public class PlayerPhonePatchLI
    {
        [HarmonyPatch("UpdatePhoneSanity")]
        [HarmonyPrefix]
        static bool UpdatePhoneSanity_PreFix(PlayerControllerB playerController)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
            if (internAI != null)
            {
                return false;
            }

            Transform? phoneTransform = playerController.transform.Find("PhonePrefab(Clone)");
            if (phoneTransform == null)
            {
                return false;
            }

            PlayerPhone? playerPhone = phoneTransform.GetComponent<PlayerPhone>();
            if (playerPhone == null)
            {
                return false;
            }

            return true;
        }
    }
}

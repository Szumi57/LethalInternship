﻿using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
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
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)playerController.playerClientId);
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

﻿using HarmonyLib;
using LethalInternship.Managers;
using System.Collections.Generic;
using Unity.Netcode;

namespace LethalInternship.Patches.GameEnginePatches
{
    [HarmonyPatch(typeof(NetworkSceneManager))]
    [HarmonyAfter(Const.MORECOMPANY_GUID)]
    internal class NetworkSceneManagerPatch
    {
        [HarmonyPatch("PopulateScenePlacedObjects")]
        [HarmonyPostfix]
        public static void PopulateScenePlacedObjects_Postfix(Dictionary<uint, Dictionary<int, NetworkObject>> ___ScenePlacedObjects)
        {
            //InternManager.Instance.ResizeAndPopulateInterns(___ScenePlacedObjects);
        }
    }
}

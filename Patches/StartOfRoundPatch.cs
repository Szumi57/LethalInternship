using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using static UnityEngine.UIElements.UIR.Implementation.UIRStylePainter;
using System.Runtime.CompilerServices;
using System.Linq;
using LethalInternship.Utils;
using GameNetcodeStuff;
using UnityEngine.AI;
using LethalInternship.AI;

namespace LethalInternship.Patches
{
    //todo SUPPRIMER ?
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start_PostFix(ref StartOfRound __instance)
        {
            if (!__instance.IsServer)
            {
                return;
            }
        }

        
    }
}

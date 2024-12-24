using GameNetcodeStuff;
using LethalInternship.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LethalInternship.Patches.ModPatches.ButteryFixes
{
    internal class BodyPatchesPatch
    {
        public static bool DeadBodyInfoPostStart_Prefix(DeadBodyInfo __0)
        {
            if (__0.playerScript != null 
                && InternManager.Instance.IsPlayerIntern(__0.playerScript))
            {
                return false;
            }
            return true;
        }
    }
}

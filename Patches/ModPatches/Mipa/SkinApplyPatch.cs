using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;

namespace LethalInternship.Patches.ModPatches.Mipa
{
    [HarmonyPatch(typeof(SkinApply))]
    public class SkinApplyPatch
    {
        [HarmonyPatch("UpdateTalking")]
        [HarmonyPrefix]
        static bool FixedUpdate_Prefix(SkinApply __instance)
        {
            InternAI? internAI = InternManager.Instance.GetInternAI((int)__instance.m_Player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            __instance.m_IsTalking = internAI.InternIdentity.Voice.IsTalking();
            return false;
        }
    }
}

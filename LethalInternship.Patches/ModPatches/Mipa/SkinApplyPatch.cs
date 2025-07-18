using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.ModPatches.Mipa
{
    [HarmonyPatch(typeof(SkinApply))]
    public class SkinApplyPatch
    {
        [HarmonyPatch("UpdateTalking")]
        [HarmonyPrefix]
        static bool FixedUpdate_Prefix(SkinApply __instance)
        {
            IInternAI? internAI = InternManagerProvider.Instance.GetInternAI((int)__instance.m_Player.playerClientId);
            if (internAI == null)
            {
                return true;
            }

            __instance.m_IsTalking = internAI.InternIdentity.Voice.IsTalking();
            return false;
        }
    }
}

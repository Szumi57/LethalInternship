using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(RagdollGrabbableObject))]
    internal class RagdollGrabbableObjectPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_PreFix(RagdollGrabbableObject __instance)
        {
            InternAI? internAI = InternManager.Instance.GetInternAiOfRagdollBody(__instance);
            if (internAI == null)
            {
                return true;
            }

            if (internAI.RagdollInternBody == null)
            {
                Plugin.LogDebug("internAI.RagdollInternBody is null !");
                return true;
            }

            internAI.RagdollInternBody.Update();
            return false;
        }
    }
}

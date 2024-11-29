using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Constants;

namespace LethalInternship.Patches.ObjectsPatches
{
    [HarmonyPatch(typeof(RagdollGrabbableObject))]
    internal class RagdollGrabbableObjectPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool Update_PreFix(RagdollGrabbableObject __instance, ref bool ___foundRagdollObject)
        {
            int bodyID = __instance.bodyID.Value;
            if (bodyID == Const.INIT_RAGDOLL_ID)
            {
                RagdollInternBody.Update_Patch(__instance);
                return false;
            }

            // BodyId is a networkVariable
            // It is 0 until the client receive the message that it is not
            // but the init of RagdollGrabbableObject can be done with that value not yet updated
            // So we make sure that the deadbody id still match the bodyID network variable in case it has been updated
            if (___foundRagdollObject)
            {
                if (StartOfRound.Instance.allPlayerScripts[bodyID].deadBody != null
                    && __instance.ragdoll != null
                    && bodyID != (int)__instance.ragdoll.playerScript.playerClientId)
                {
                    ___foundRagdollObject = false;
                }
            }

            return true;
        }
    }
}

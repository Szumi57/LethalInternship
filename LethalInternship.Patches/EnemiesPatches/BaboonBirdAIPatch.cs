using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for the <c>BaboonBirdAI</c>
    /// </summary>
    [HarmonyPatch(typeof(BaboonBirdAI))]
    public class BaboonBirdAIPatch
    {
        /// <summary>
        /// Patch to make the baboon not feel threatened by intern
        /// </summary>
        [HarmonyPatch("ReactToThreat")]
        [HarmonyPrefix]
        static bool ReactToThreat_PreFix(Threat closestThreat)
        {
            PlayerControllerB playerController = closestThreat.threatScript.GetThreatTransform().gameObject.GetComponent<PlayerControllerB>();
            if (playerController != null)
            {
                if (InternManagerProvider.Instance.IsPlayerIntern(playerController))
                {
                    // Stop reacting to threat if intern
                    return false;
                }
            }

            // Intern true, continue with base game method
            // Else stop reacting to threat
            return closestThreat.threatScript.GetThreatTransform().gameObject.GetComponent<IInternAI>() == null;
        }
    }
}

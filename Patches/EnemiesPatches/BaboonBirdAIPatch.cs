using GameNetcodeStuff;
using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;

namespace LethalInternship.Patches.EnemiesPatches
{
    /// <summary>
    /// Patch for the <c>BaboonBirdAI</c>
    /// </summary>
    [HarmonyPatch(typeof(BaboonBirdAI))]
    internal class BaboonBirdAIPatch
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
                if (InternManager.Instance.IsPlayerIntern(playerController))
                {
                    // Stop reacting to threat if intern
                    return false;
                }
            }

            // Intern true, continue with base game method
            // Else stop reacting to threat
            return closestThreat.threatScript.GetThreatTransform().gameObject.GetComponent<InternAI>() == null;
        }
    }
}

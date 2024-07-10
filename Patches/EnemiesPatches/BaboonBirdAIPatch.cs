using GameNetcodeStuff;
using HarmonyLib;
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
            if (closestThreat.type != ThreatType.Player)
            {
                // continue with base game method
                return true;
            }

            PlayerControllerB playerController = closestThreat.threatScript.GetThreatTransform().gameObject.GetComponent<PlayerControllerB>();
            if (playerController == null)
            {
                // continue with base game method
                return true;
            }

            if (InternManager.Instance.IsPlayerIntern(playerController))
            {
                // Stop reacting to threat if intern
                return false;
            }

            return true;
        }
    }
}

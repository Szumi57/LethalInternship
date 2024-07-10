using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using Unity.Netcode;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>NetworkObject</c>
    /// </summary>
    [HarmonyPatch(typeof(NetworkObject))]
    internal class NetworkObjectPatch
    {
        /// <summary>
        /// Patch for intercepting the change of ownership on a network object.
        /// If the owner ship goes to an intern, it should go to the owner of the intern
        /// </summary>
        /// <remarks>
        /// Patch maybe useless with the change of method for grabbing object for an intern
        /// </remarks>
        /// <param name="newOwnerClientId"></param>
        /// <returns></returns>
        [HarmonyPatch("ChangeOwnership")]
        [HarmonyPrefix]
        static bool ChangeOwnership_PreFix(ref ulong newOwnerClientId)
        {
            Plugin.LogDebug($"Try network object ChangeOwnership newOwnerClientId : {(int)newOwnerClientId}");
            InternAI? internAI = InternManager.Instance.GetInternAI((int)newOwnerClientId);
            if (internAI != null)
            {
                Plugin.LogDebug($"network ChangeOwnership not on intern but on intern owner : {internAI.OwnerClientId}");
                newOwnerClientId = internAI.OwnerClientId;
            }
            return true;
        }
    }
}

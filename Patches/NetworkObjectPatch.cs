using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Managers;
using Unity.Netcode;

namespace LethalInternship.Patches
{
    [HarmonyPatch(typeof(NetworkObject))]
    internal class NetworkObjectPatch
    {
        [HarmonyPatch("ChangeOwnership")]
        [HarmonyPrefix]
        static bool ChangeOwnership_PreFix(ref ulong newOwnerClientId)
        {
            Plugin.Logger.LogDebug($"Try network object ChangeOwnership newOwnerClientId : {(int)newOwnerClientId}");
            InternAI? internAI = InternManager.GetInternAI((int)newOwnerClientId);
            if (internAI != null)
            {
                Plugin.Logger.LogDebug($"network ChangeOwnership not on intern but on intern owner : {internAI.OwnerClientId}");
                newOwnerClientId = internAI.OwnerClientId;
            }
            return true;
        }
    }
}

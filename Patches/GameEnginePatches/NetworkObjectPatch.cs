using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Constants;
using LethalInternship.Managers;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Patches.GameEnginePatches
{
    /// <summary>
    /// Patch for <c>NetworkObject</c>
    /// </summary>
    [HarmonyPatch(typeof(NetworkObject))]
    public class NetworkObjectPatch
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
            if(newOwnerClientId > Const.INTERN_ACTUAL_ID_OFFSET)
            {
                InternAI? internAI = InternManager.Instance.GetInternAI((int)(newOwnerClientId - Const.INTERN_ACTUAL_ID_OFFSET));
                if (internAI != null)
                {
                    Plugin.LogDebug($"network ChangeOwnership not on intern but on intern owner : {internAI.OwnerClientId}");
                    newOwnerClientId = internAI.OwnerClientId;
                }
            }
            
            return true;
        }

        [HarmonyPatch("OnTransformParentChanged")]
        [HarmonyPrefix]
        static bool OnTransformParentChanged_PreFix(NetworkObject __instance,
                                                    Transform ___m_CachedParent)
        {
            if (!DebugConst.SHOW_LOG_DEBUG_ONTRANSFORMPARENTCHANGED)
            {
                return true;
            }

            if (!__instance.AutoObjectParentSync)
            {
                return true;
            }

            if (__instance.transform.parent == ___m_CachedParent)
            {
                return true;
            }
            if (__instance.NetworkManager == null || !__instance.NetworkManager.IsListening)
            {
                return true;
            }
            if (!__instance.NetworkManager.IsServer)
            {
                return true;
            }
            if (!__instance.IsSpawned)
            {
                return true;
            }

            Transform parent = __instance.transform.parent;
            if (parent != null)
            {
                NetworkObject networkObject;
                if (!__instance.transform.parent.TryGetComponent<NetworkObject>(out networkObject))
                {
                    Plugin.LogDebug($"{__instance.transform.parent} Invalid parenting, NetworkObject moved under a non-NetworkObject parent");
                    return true;
                }
                if (!networkObject.IsSpawned)
                {
                    Plugin.LogDebug($"{networkObject} {networkObject.name} NetworkObject can only be reparented under another spawned NetworkObject");
                    return true;
                }
            }

            return true;
        }
    }
}

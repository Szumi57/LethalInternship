using HarmonyLib;
using LethalInternship.Core.Managers;
using LethalInternship.Managers;
using LethalInternship.SharedAbstractions.ManagerProviders;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.PluginPatches.GameEnginePatches
{
    /// <summary>
    /// Patches for <c>StartOfRound</c>
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatch
    {
        /// <summary>
        /// Load the managers if the client is host/server
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static void Awake_Prefix(StartOfRound __instance)
        {
            Plugin.LogDebug("Initialize managers...");

            GameObject objectManager;

            // MonoBehaviours
            objectManager = new GameObject("InputManager");
            objectManager.AddComponent<InputManager>();

            objectManager = new GameObject("AudioManager");
            objectManager.AddComponent<AudioManager>();

            objectManager = new GameObject("IdentityManager");
            objectManager.AddComponent<IdentityManager>();

            objectManager = new GameObject("UIManager");
            objectManager.AddComponent<UIManager>();

            // NetworkBehaviours
            objectManager = Object.Instantiate(PluginManager.Instance.TerminalManagerPrefab);
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                objectManager.GetComponent<NetworkObject>().Spawn();
            }

            objectManager = Object.Instantiate(PluginManager.Instance.SaveManagerPrefab);
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                objectManager.GetComponent<NetworkObject>().Spawn();
            }

            objectManager = Object.Instantiate(PluginManager.Instance.InternManagerPrefab);
            if (__instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer)
            {
                objectManager.GetComponent<NetworkObject>().Spawn();
            }

            // Initialize managers for solution
            InternManagerProvider.Instance = InternManager.Instance;
            IdentityManagerProvider.Instance = IdentityManager.Instance;
            InputManagerProvider.Instance = InputManager.Instance;
            SaveManagerProvider.Instance = SaveManager.Instance;
            TerminalManagerProvider.Instance = TerminalManager.Instance;
            UIManagerProvider.Instance = UIManager.Instance;

            Plugin.LogDebug("... Managers started");
        }
    }
}

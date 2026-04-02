using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.Managers;
using UnityEngine;

namespace LethalInternship.Managers
{
    /// <summary>
    /// Manager in charge of initializing network behaviours for LethalInternship
    /// </summary>
    internal class PluginManager : MonoBehaviour, IPluginManager
    {
        private static PluginManager _instance = null!;
        public static PluginManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(PluginManager));
                    _instance = go.AddComponent<PluginManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private GameObject saveManagerPrefab = null!;
        private GameObject internManagerPrefab = null!;
        private GameObject terminalManagerPrefab = null!;

        public GameObject SaveManagerPrefab => saveManagerPrefab;
        public GameObject InternManagerPrefab => internManagerPrefab;
        public GameObject TerminalManagerPrefab => terminalManagerPrefab;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            PluginManagerProvider.Register(this);
        }

        /// <summary>
        /// Create a network prefab with <c>LethalLib</c> and add the component manager to it, which initialize it (but not spawn it network wise)
        /// </summary>
        /// <remarks>
        /// For spawning manager over the network see <see cref="Patches.GameEnginePatches.StartOfRoundPatch.Awake_Prefix"><c>StartOfRoundPatch.Awake_Prefix</c></see>
        /// </remarks>
        public void InitManagers()
        {
            internManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("InternManager");
            internManagerPrefab.AddComponent<InternManager>();

            saveManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("SaveManager");
            saveManagerPrefab.AddComponent<SaveManager>();

            terminalManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("TerminalManager");
            terminalManagerPrefab.AddComponent<TerminalManager>();
        }
    }
}

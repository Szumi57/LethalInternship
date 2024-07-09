using UnityEngine;

namespace LethalInternship.Managers
{
    /// <summary>
    /// Manager in charge of initializing other managers for LethalInternship
    /// </summary>
    internal class PluginManager : MonoBehaviour
    {
        public static PluginManager Instance { get; private set; } = null!;

        /// <summary>
        /// <c>GameObject</c> prefab of the <c>SaveManager</c>, see: <see cref="SaveManager"><c>SaveManager</c></see>
        /// </summary>
        public GameObject SaveManagerPrefab = null!;
        /// <summary>
        /// <c>GameObject</c> prefab of the <c>InternManager</c>, see: <see cref="InternManager"><c>InternManager</c></see>
        /// </summary>
        public GameObject InternManagerPrefab = null!;
        /// <summary>
        /// <c>GameObject</c> prefab of the <c>TerminalManager</c>, see: <see cref="TerminalManager"><c>TerminalManager</c></see>
        /// </summary>
        public GameObject TerminalManagerPrefab = null!;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Create a network prefab with <c>LethalLib</c> and add the component manager to it, which initialize it (but not spawn it network wise)
        /// </summary>
        /// <remarks>
        /// For spawning manager over the network see <see cref="Patches.GameEnginePatches.StartOfRoundPatch.Awake_Prefix"><c>StartOfRoundPatch.Awake_Prefix</c></see>
        /// </remarks>
        public void InitManagers()
        {
            InternManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("InternManager");
            InternManagerPrefab.AddComponent<InternManager>();

            SaveManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("SaveManager");
            SaveManagerPrefab.AddComponent<SaveManager>();
            
            TerminalManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("TerminalManager");
            TerminalManagerPrefab.AddComponent<TerminalManager>();
        }
    }
}

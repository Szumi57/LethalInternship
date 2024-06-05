using MoreCompany;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Managers
{
    internal class PluginManager : MonoBehaviour
    {
        public static PluginManager Instance { get; private set; } = null!;

        public GameObject SaveManagerPrefab = null!;
        public GameObject InternManagerPrefab = null!;
        public GameObject TerminalManagerPrefab = null!;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

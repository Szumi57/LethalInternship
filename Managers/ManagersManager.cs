using MoreCompany;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalInternship.Managers
{
    internal class ManagersManager : MonoBehaviour
    {
        public static ManagersManager Instance { get; private set; } = null!;

        public GameObject TerminalManagerPrefab = null!;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void InitManagers()
        {
            InternManager.Init();

            TerminalManagerPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("TerminalManager");
            TerminalManagerPrefab.AddComponent<TerminalManager>();
        }
    }
}

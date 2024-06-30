using LethalInternship.Managers.SaveInfos;
using Newtonsoft.Json;
using System;
using Unity.Netcode;

namespace LethalInternship.Managers
{
    internal class SaveManager : NetworkBehaviour
    {
        private const string SAVE_DATA_KEY = "LETHAL_INTERNSHIP_SAVE_DATA";

        public static SaveManager Instance { get; private set; } = null!;

        public SaveFile Save { get; internal set; } = null!;

        private ClientRpcParams ClientRpcParams = new ClientRpcParams();

        private void Awake()
        {
            Instance = this;
            if (NetworkManager.IsHost)
            {
                FetchSaveFile();
                LoadInfosInSave();
                Plugin.Logger.LogDebug($"Init NbInternsOwned to {InternManager.Instance.NbInternsOwned}.");
            }
        }

        public void SavePluginInfos()
        {
            if (!NetworkManager.IsHost)
            {
                return;
            }

            Plugin.Logger.LogInfo($"Saving data for LethalInternship plugin.");
            string saveFile = GameNetworkManager.Instance.currentSaveFileName;
            SaveInfosInSave();
            string json = JsonConvert.SerializeObject(Save);
            ES3.Save(key: SAVE_DATA_KEY, value: json, filePath: saveFile);
        }

        private void SaveInfosInSave()
        {
            Save.NbInternOwned = InternManager.Instance.NbInternsOwned;
        }

        private void LoadInfosInSave()
        {
            InternManager.Instance.NbInternsOwned = Save.NbInternOwned;
            InternManager.Instance.NbInternsToDropShip = Save.NbInternOwned;
        }

        private void FetchSaveFile()
        {
            string saveFile = GameNetworkManager.Instance.currentSaveFileName;

            try
            {
                string json = (string)ES3.Load(key: SAVE_DATA_KEY, defaultValue: null, filePath: saveFile);
                if (json != null)
                {
                    Plugin.Logger.LogInfo($"Loading save file.");
                    Save = JsonConvert.DeserializeObject<SaveFile>(json) ?? new SaveFile();
                }
                else
                {
                    Plugin.Logger.LogInfo($"No save file found for slot. Creating new.");
                    Save = new SaveFile();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error when loading save file : {ex.Message}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncNbInternsOwnedServerRpc(ulong clientId)
        {
            ClientRpcParams.Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientId }
            };

            SyncNbInternsOwnedClientRpc(InternManager.Instance.NbInternsOwned, InternManager.Instance.NbInternsToDropShip, ClientRpcParams);
        }

        [ClientRpc]
        private void SyncNbInternsOwnedClientRpc(int nbInternsOwned, int NbInternsToDropShip, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
            {
                return;
            }

            Plugin.Logger.LogInfo($"Client: sync interns alive and ready to {nbInternsOwned}, NbInternsToDropShip {NbInternsToDropShip} client execute...");
            InternManager.Instance.NbInternsOwned = nbInternsOwned;
            InternManager.Instance.NbInternsToDropShip = NbInternsToDropShip;
        }
    }
}

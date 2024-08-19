using LethalInternship.Managers.SaveInfos;
using Newtonsoft.Json;
using System;
using Unity.Netcode;

namespace LethalInternship.Managers
{
    /// <summary>
    /// Manager in charge of loading and saving data relevant to the mod LethalInternship
    /// </summary>
    internal class SaveManager : NetworkBehaviour
    {
        private const string SAVE_DATA_KEY = "LETHAL_INTERNSHIP_SAVE_DATA";

        public static SaveManager Instance { get; private set; } = null!;

        private SaveFile Save = null!;
        private ClientRpcParams ClientRpcParams = new ClientRpcParams();

        /// <summary>
        /// When manager awake, read the save file and load infos for LethalInternship, only for the host
        /// </summary>
        private void Awake()
        {
            Instance = this;
            if (NetworkManager.IsHost)
            {
                FetchSaveFile();
                LoadInfosInSave();
                Plugin.LogDebug($"Init NbInternsOwned to {InternManager.Instance.NbInternsOwned}. Landing status allowed : {InternManager.Instance.LandingStatusAllowed}");
            }
        }

        /// <summary>
        /// Get save file, serialize save data and save it using <see cref="SAVE_DATA_KEY"><c>SAVE_DATA_KEY</c></see>, only host
        /// </summary>
        public void SavePluginInfos()
        {
            if (!NetworkManager.IsHost)
            {
                return;
            }

            Plugin.LogInfo($"Saving data for LethalInternship plugin.");
            string saveFile = GameNetworkManager.Instance.currentSaveFileName;
            SaveInfosInSave();
            string json = JsonConvert.SerializeObject(Save);
            ES3.Save(key: SAVE_DATA_KEY, value: json, filePath: saveFile);
        }

        /// <summary>
        /// Update save data with runtime data from managers
        /// </summary>
        private void SaveInfosInSave()
        {
            Save.NbInternOwned = InternManager.Instance.NbInternsOwned;
            Save.LandingStatusAborted = !InternManager.Instance.LandingStatusAllowed;
        }

        /// <summary>
        /// Load data into managers from save data
        /// </summary>
        private void LoadInfosInSave()
        {
            int maxInternsAvailable = Plugin.Config.MaxInternsAvailable.Value;
            InternManager.Instance.NbInternsOwned = maxInternsAvailable < Save.NbInternOwned ? maxInternsAvailable : Save.NbInternOwned;
            InternManager.Instance.NbInternsToDropShip = InternManager.Instance.NbInternsOwned;
            InternManager.Instance.LandingStatusAllowed = !Save.LandingStatusAborted;
        }

        /// <summary>
        /// Get the save file and load it into the save data, or create a new one if no save file found
        /// </summary>
        private void FetchSaveFile()
        {
            string saveFile = GameNetworkManager.Instance.currentSaveFileName;

            try
            {
                string json = (string)ES3.Load(key: SAVE_DATA_KEY, defaultValue: null, filePath: saveFile);
                if (json != null)
                {
                    Plugin.LogInfo($"Loading save file.");
                    Save = JsonConvert.DeserializeObject<SaveFile>(json) ?? new SaveFile();
                }
                else
                {
                    Plugin.LogInfo($"No save file found for slot. Creating new.");
                    Save = new SaveFile();
                }
            }
            catch (Exception ex)
            {
                Plugin.LogError($"Error when loading save file : {ex.Message}");
            }
        }

        #region Sync loaded save file

        /// <summary>
        /// Send to the specific client, the data load by the server/host, so the client can initialize its managers
        /// </summary>
        /// <remarks>
        /// Only the host loads the data from the file, so the clients needs to request the server/host for the save data to syn
        /// </remarks>
        /// <param name="clientId">Client id of caller</param>
        [ServerRpc(RequireOwnership = false)]
        public void SyncLoadedSaveInfosServerRpc(ulong clientId)
        {
            Plugin.LogDebug($"Client {clientId} ask server/host {NetworkManager.LocalClientId} to SyncLoadedSaveInfos");
            ClientRpcParams.Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientId }
            };

            SyncLoadedSaveInfosClientRpc(InternManager.Instance.NbInternsOwned, 
                                         InternManager.Instance.NbInternsToDropShip, 
                                         InternManager.Instance.LandingStatusAllowed,
                                         ClientRpcParams);
        }

        /// <summary>
        /// Client side, sync the save data send by the server/host
        /// </summary>
        /// <param name="nbInternsOwned"></param>
        /// <param name="NbInternsToDropShip"></param>
        /// <param name="clientRpcParams"></param>
        [ClientRpc]
        private void SyncLoadedSaveInfosClientRpc(int nbInternsOwned, 
                                                 int NbInternsToDropShip,
                                                 bool landingAllowed,
                                                 ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
            {
                return;
            }

            Plugin.LogInfo($"Client {NetworkManager.LocalClientId} : sync interns alive and ready to {nbInternsOwned}, NbInternsToDropShip {NbInternsToDropShip}, landingAllowed {landingAllowed}, client execute...");
            InternManager.Instance.NbInternsOwned = nbInternsOwned;
            InternManager.Instance.NbInternsToDropShip = NbInternsToDropShip;
            InternManager.Instance.LandingStatusAllowed = landingAllowed;
        }

        #endregion
    }
}

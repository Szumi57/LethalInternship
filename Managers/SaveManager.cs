using LethalInternship.AI;
using LethalInternship.Enums;
using LethalInternship.NetworkSerializers;
using LethalInternship.SaveAdapter;
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
            FetchSaveFile();
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

        /// <summary>
        /// Get save file, serialize save data and save it using <see cref="SAVE_DATA_KEY"><c>SAVE_DATA_KEY</c></see>, only host
        /// </summary>
        public void SavePluginInfos()
        {
            if (!NetworkManager.IsHost)
            {
                return;
            }

            if (!StartOfRound.Instance.inShipPhase)
            {
                return;
            }
            if (StartOfRound.Instance.isChallengeFile)
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
            Save.LandingStatusAborted = !InternManager.Instance.LandingStatusAllowed;

            Save.IdentitiesSaveFiles = new IdentitySaveFile[IdentityManager.Instance.InternIdentities.Length];
            for (int i = 0; i < IdentityManager.Instance.InternIdentities.Length; i++)
            {
                InternIdentity internIdentity = IdentityManager.Instance.InternIdentities[i];
                IdentitySaveFile identitySaveFile = new IdentitySaveFile()
                {
                    IdIdentity = internIdentity.IdIdentity,
                    Hp = internIdentity.Hp,
                    SuitID = internIdentity.SuitID.HasValue ? internIdentity.SuitID.Value : -1,
                    Status = (int)internIdentity.Status
                };

                Plugin.LogDebug($"Saving identity {internIdentity.ToString()}");
                Save.IdentitiesSaveFiles[i] = identitySaveFile;
            }
        }

        /// <summary>
        /// Load data into managers from save data
        /// </summary>
        public void LoadDataFromSave()
        {
            InternManager.Instance.LandingStatusAllowed = !Save.LandingStatusAborted;
            Plugin.LogDebug($"Loaded from save Landing status allowed : {InternManager.Instance.LandingStatusAllowed}");

            if (Save.IdentitiesSaveFiles != null)
            {
                for (int i = 0; i < Save.IdentitiesSaveFiles.Length; i++)
                {
                    if (i >= IdentityManager.Instance.InternIdentities.Length)
                    {
                        break;
                    }

                    IdentitySaveFile identitySaveFile = Save.IdentitiesSaveFiles[i];

                    InternIdentity internIdentity = IdentityManager.Instance.InternIdentities[identitySaveFile.IdIdentity];
                    internIdentity.Hp = identitySaveFile.Hp;
                    internIdentity.SuitID = identitySaveFile.SuitID < 0 ? null : identitySaveFile.SuitID;
                    internIdentity.Status = (EnumStatusIdentity)identitySaveFile.Status;
                    Plugin.LogDebug($"Loaded and updated identity from save {internIdentity.ToString()}");
                }
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

            IdentitySaveFileNetworkSerializable[] identitiesSaveNS = new IdentitySaveFileNetworkSerializable[IdentityManager.Instance.InternIdentities.Length];
            for (int i = 0; i < identitiesSaveNS.Length; i++)
            {
                InternIdentity internIdentity = IdentityManager.Instance.InternIdentities[i];
                IdentitySaveFileNetworkSerializable identitySaveNS = new IdentitySaveFileNetworkSerializable()
                {
                    IdIdentity = internIdentity.IdIdentity,
                    Hp = internIdentity.Hp,
                    SuitID = internIdentity.SuitID.HasValue ? internIdentity.SuitID.Value : -1,
                    Status = (int)internIdentity.Status
                };

                identitiesSaveNS[i] = identitySaveNS;
            }

            SaveNetworkSerializable saveNS = new SaveNetworkSerializable()
            {
                LandingAllowed = InternManager.Instance.LandingStatusAllowed,
                Identities = identitiesSaveNS
            };

            SyncLoadedSaveInfosClientRpc(saveNS, ClientRpcParams);
        }

        /// <summary>
        /// Client side, sync the save data send by the server/host
        /// </summary>
        /// <param name="clientRpcParams"></param>
        [ClientRpc]
        private void SyncLoadedSaveInfosClientRpc(SaveNetworkSerializable saveNetworkSerializable,
                                                  ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
            {
                return;
            }

            Plugin.LogDebug($"Client {NetworkManager.LocalClientId} : sync in save file landingAllowed {saveNetworkSerializable.LandingAllowed}");
            Save.LandingStatusAborted = !saveNetworkSerializable.LandingAllowed;

            Save.IdentitiesSaveFiles = new IdentitySaveFile[saveNetworkSerializable.Identities.Length];
            for (int i = 0; i < Save.IdentitiesSaveFiles.Length; i++)
            {
                IdentitySaveFileNetworkSerializable identitySaveNS = saveNetworkSerializable.Identities[i];
                IdentitySaveFile identitySaveFile = new IdentitySaveFile()
                {
                    IdIdentity = identitySaveNS.IdIdentity,
                    Hp = identitySaveNS.Hp,
                    SuitID = identitySaveNS.SuitID,
                    Status = identitySaveNS.Status
                };

                Plugin.LogDebug($"Client {NetworkManager.LocalClientId} : sync in save file, identity {identitySaveFile.ToString()}");
                Save.IdentitiesSaveFiles[i] = identitySaveFile;
            }

            LoadDataFromSave();
        }

        #endregion
    }
}

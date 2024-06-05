using LethalInternship.Managers.SaveInfos;
using Newtonsoft.Json;
using Unity.Netcode;

namespace LethalInternship.Managers
{
    internal class SaveManager : NetworkBehaviour
    {
        private const string SAVE_DATA_KEY = "LETHAL_INTERNSHIP_SAVE_DATA";

        public static SaveManager Instance { get; private set; } = null!;

        public SaveFile Save { get; internal set; } = null!;

        private void Awake()
        {
            Instance = this;
            if (NetworkManager.IsHost)
            {
                FetchSaveFile();
                InternManager.Instance.NbInternsToDropShip = Save.NbInternAliveAndOrdered;
                SyncNbInternsToDropShipFromServerToClientRpc(InternManager.Instance.NbInternsToDropShip);
                Plugin.Logger.LogDebug($"Init NbInternsToDropShip to {InternManager.Instance.NbInternsToDropShip}.");
            }
        }

        public void SavePluginInfos()
        {
            Plugin.Logger.LogInfo($"Saving data fro LethalInternship plugin.");
            string saveFile = GameNetworkManager.Instance.currentSaveFileName;
            Save.NbInternAliveAndOrdered = InternManager.Instance.NbInternsToDropShip;
            string json = JsonConvert.SerializeObject(Save);
            ES3.Save(key: SAVE_DATA_KEY, value: json, filePath: saveFile);
        }

        private void FetchSaveFile()
        {
            string saveFile = GameNetworkManager.Instance.currentSaveFileName;
            
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

        [ClientRpc]
        private void SyncNbInternsToDropShipFromServerToClientRpc(int nbInternsBoughtSaveFromServer)
        {
            Plugin.Logger.LogInfo($"Server send to clients to sync interns alive and ready to ${nbInternsBoughtSaveFromServer}, client execute...");
            InternManager.Instance.NbInternsToDropShip = nbInternsBoughtSaveFromServer;
        }
    }
}

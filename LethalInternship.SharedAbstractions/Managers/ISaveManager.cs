using UnityEngine;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface ISaveManager
    {
        GameObject ManagerGameObject { get; }

        void SavePluginInfos();
        void SyncCurrentValuesServerRpc(ulong clientId);
    }
}

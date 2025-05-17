namespace LethalInternship.SharedAbstractions.Managers
{
    public interface ISaveManager
    {
        void SavePluginInfos();
        void SyncCurrentValuesServerRpc(ulong clientId);
    }
}

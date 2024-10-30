using Unity.Netcode;

namespace LethalInternship.Configs
{
    internal struct ConfigIdentityNetworkSerializable : INetworkSerializable
    {
        public ConfigIdentity[] ConfigIdentities;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ConfigIdentities);
        }
    }
}

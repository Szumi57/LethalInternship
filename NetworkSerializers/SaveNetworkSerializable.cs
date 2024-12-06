using Unity.Netcode;

namespace LethalInternship.NetworkSerializers
{
    internal struct SaveNetworkSerializable : INetworkSerializable
    {
        public bool LandingAllowed;
        public IdentitySaveFileNetworkSerializable[] Identities;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref LandingAllowed);
            serializer.SerializeValue(ref Identities);
        }
    }

    internal struct IdentitySaveFileNetworkSerializable : INetworkSerializable
    {
        public int IdIdentity;
        public int SuitID;
        public int Hp;
        public int Status;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IdIdentity);
            serializer.SerializeValue(ref SuitID);
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref Status);
        }
    }
}

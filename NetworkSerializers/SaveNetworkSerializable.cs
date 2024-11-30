using Unity.Netcode;

namespace LethalInternship.NetworkSerializers
{
    internal struct SaveNetworkSerializable : INetworkSerializable
    {
        public int NbInternsOwned;
        public int NbInternsToDropShip;
        public bool LandingAllowed;
        public IdentitySaveFileNetworkSerializable[] Identities;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref NbInternsOwned);
            serializer.SerializeValue(ref NbInternsToDropShip);
            serializer.SerializeValue(ref LandingAllowed);
            serializer.SerializeValue(ref Identities);
        }
    }

    internal struct IdentitySaveFileNetworkSerializable : INetworkSerializable
    {
        public int IdIdentity;
        public int SuitID;
        public int Hp;
        public bool SelectedToDrop;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IdIdentity);
            serializer.SerializeValue(ref SuitID);
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref SelectedToDrop);
        }
    }
}

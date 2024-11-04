using Unity.Netcode;

namespace LethalInternship.NetworkSerializers
{
    public struct SaveNetworkSerializable : INetworkSerializable
    {
        public int NbInternsOwned;
        public int NbInternsToDropShip;
        public bool LandingAllowed;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref NbInternsOwned);
            serializer.SerializeValue(ref NbInternsToDropShip);
            serializer.SerializeValue(ref LandingAllowed);
        }
    }
}

using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.NetworkSerializers
{
    public struct PlaceItemNetworkSerializable : INetworkSerializable
    {
        public NetworkObjectReference GrabbedObject;
        public NetworkObjectReference ParentObject;
        public Vector3 PlacePositionOffset;
        public bool MatchRotationOfParent;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref GrabbedObject);
            serializer.SerializeValue(ref ParentObject);
            serializer.SerializeValue(ref PlacePositionOffset);
            serializer.SerializeValue(ref MatchRotationOfParent);
        }
    }
}

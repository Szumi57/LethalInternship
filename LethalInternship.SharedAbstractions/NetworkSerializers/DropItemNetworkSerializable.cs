using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.NetworkSerializers
{
    public struct DropItemNetworkSerializable : INetworkSerializable
    {
        public NetworkObjectReference GrabbedObject;
        public bool DroppedInElevator;
        public bool DroppedInShipRoom;
        public Vector3 TargetFloorPosition;
        public int FloorYRot;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref GrabbedObject);
            serializer.SerializeValue(ref DroppedInElevator);
            serializer.SerializeValue(ref DroppedInShipRoom);
            serializer.SerializeValue(ref TargetFloorPosition);
            serializer.SerializeValue(ref FloorYRot);
        }
    }
}

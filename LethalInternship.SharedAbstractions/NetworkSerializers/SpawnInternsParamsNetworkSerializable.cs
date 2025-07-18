using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.NetworkSerializers
{
    public struct SpawnInternsParamsNetworkSerializable : INetworkSerializable
    {
        public int IndexNextIntern;
        public int IndexNextPlayerObject;
        public int InternIdentityID;
        public int Hp;
        public int SuitID;
        public int enumSpawnAnimation;
        public Vector3 SpawnPosition;
        public float YRot;
        public bool IsOutside;
        public bool ShouldDestroyDeadBody;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IndexNextIntern);
            serializer.SerializeValue(ref IndexNextPlayerObject);
            serializer.SerializeValue(ref InternIdentityID);
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref SuitID);
            serializer.SerializeValue(ref enumSpawnAnimation);
            serializer.SerializeValue(ref SpawnPosition);
            serializer.SerializeValue(ref YRot);
            serializer.SerializeValue(ref IsOutside);
            serializer.SerializeValue(ref ShouldDestroyDeadBody);
        }
    }
}

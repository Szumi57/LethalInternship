using LethalInternship.SharedAbstractions.Enums;
using System;
using Unity.Netcode;

namespace LethalInternship.SharedAbstractions.NetworkSerializers
{
    [Serializable]
    public struct ConfigIdentity : INetworkSerializable
    {
        public string name;
        public int suitID;
        public int suitConfigOption;
        public string voiceFolder;
        public float volume;
        public float voicePitch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref suitID);
            serializer.SerializeValue(ref suitConfigOption);
            serializer.SerializeValue(ref voiceFolder);
            serializer.SerializeValue(ref volume);
            serializer.SerializeValue(ref voicePitch);
        }

        public override string ToString()
        {
            return $"name: {name}, suitID {suitID}, suitConfigOption {suitConfigOption} {(EnumOptionSuitConfig)suitConfigOption}, voiceFolder {voiceFolder}, volume {volume}, voicePitch {voicePitch}";
        }
    }

    [Serializable]
    public struct ConfigIdentities
    {
        public ConfigIdentity[] configIdentities;
    }

    public struct ConfigIdentitiesNetworkSerializable : INetworkSerializable
    {
        public ConfigIdentity[] ConfigIdentities;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ConfigIdentities);
        }
    }
}

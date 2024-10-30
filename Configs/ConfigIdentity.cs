using System;
using Unity.Netcode;

namespace LethalInternship.Configs
{
    [Serializable]
    public struct ConfigIdentities
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ConfigIdentity[] configIdentities;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    }

    [Serializable]
    public struct ConfigIdentity : INetworkSerializable
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string name;
        public int suitID;
        public int suitConfigOption;
        public string voiceFolder;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref suitID);
            serializer.SerializeValue(ref suitConfigOption);
            serializer.SerializeValue(ref voiceFolder);
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public override string ToString()
        {
            return $"name: {name}, suitID {suitID}, suitConfigOption {suitConfigOption}, voiceFolder {voiceFolder}";
        }
    }
}

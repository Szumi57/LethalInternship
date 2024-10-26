using System;

namespace LethalInternship.Configs
{
    [Serializable]
    public class ConfigIdentities
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ConfigIdentity[] configIdentities;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

    [Serializable]
    public class ConfigIdentity
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string name;
        public int suitID;
        public int suitConfigOption;
        public string voiceFolder;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}

using LethalInternship.AI;
using LethalInternship.Configs;
using LethalInternship.Enums;
using System;
using UnityEngine;

namespace LethalInternship.Managers
{
    internal class IdentityManager : MonoBehaviour
    {
        public static IdentityManager Instance { get; private set; } = null!;

        public InternIdentity[] InternIdentities = null!;

        private ConfigIdentity[] configIdentities = null!;

        private void Awake()
        {
            Instance = this;
            Plugin.LogDebug("=============== awake IdentityManager =====================");
        }

        private void Update()
        {
            if (InternIdentities != null)
            {
                InternIdentity internIdentity;
                for (int i = 0; i < InternIdentities.Length; i++)
                {
                    internIdentity = InternIdentities[i];
                    if (internIdentity != null
                        && internIdentity.Voice != null)
                    {
                        internIdentity.Voice.ReduceCooldown(Time.deltaTime);
                    }
                }
            }
        }

        public void CreateIdentities(int maxInternsAvailable, ConfigIdentity[] configIdentities)
        {
            InternIdentities = new InternIdentity[maxInternsAvailable];
            this.configIdentities = configIdentities;

            // InitNewIdentity
            for (int i = 0; i < maxInternsAvailable; i++)
            {
                InternIdentities[i] = InitNewIdentity(i);
            }
        }

        private InternIdentity InitNewIdentity(int indexIntern)
        {
            // Get a config identity
            string name;
            ConfigIdentity configIdentity;
            if (indexIntern >= this.configIdentities.Length)
            {
                configIdentity = Const.DEFAULT_CONFIG_IDENTITY;
                name = string.Format(configIdentity.name, indexIntern);
            }
            else
            {
                configIdentity = this.configIdentities[indexIntern];
                name = configIdentity.name;
            }

            // Suit
            int? suitID = null;
            EnumOptionSuitConfig suitConfig;
            if (!Enum.IsDefined(typeof(EnumOptionSuitConfig), configIdentity.suitConfigOption))
            {
                Plugin.LogWarning($"Could not get option for intern suit config in config file, value {configIdentity.suitConfigOption}, name {configIdentity.name}");
                suitConfig = EnumOptionSuitConfig.AutomaticSameAsPlayer;
            }
            else
            {
                suitConfig = (EnumOptionSuitConfig)configIdentity.suitConfigOption;
            }

            switch (suitConfig)
            {
                case EnumOptionSuitConfig.AutomaticSameAsPlayer:
                case EnumOptionSuitConfig.Fixed:
                    suitID = configIdentity.suitID;
                    break;

                case EnumOptionSuitConfig.Random:
                    suitID = null;
                    break;

            }

            // Voice
            InternVoice voice = new InternVoice(name);

            // InternIdentity
            return new InternIdentity(indexIntern, name, suitID, voice);
        }
    }
}

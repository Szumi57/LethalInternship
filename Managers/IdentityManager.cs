using LethalInternship.AI;
using LethalInternship.Constants;
using LethalInternship.Enums;
using LethalInternship.NetworkSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

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
            if (InternIdentities == null)
            {
                return;
            }

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

        public void CreateIdentities(int nbIdentities, ConfigIdentity[] configIdentities)
        {
            InternIdentities = new InternIdentity[nbIdentities];
            this.configIdentities = configIdentities;

            // InitNewIdentity
            for (int i = 0; i < nbIdentities; i++)
            {
                InternIdentities[i] = InitNewIdentity(i);
            }
        }

        private InternIdentity InitNewIdentity(int idIdentity)
        {
            // Get a config identity
            string name;
            ConfigIdentity configIdentity;
            if (idIdentity >= this.configIdentities.Length)
            {
                configIdentity = Const.DEFAULT_CONFIG_IDENTITY;
                name = string.Format(configIdentity.name, idIdentity);
            }
            else
            {
                configIdentity = this.configIdentities[idIdentity];
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
            InternVoice voice = new InternVoice(configIdentity.voiceFolder,
                                                configIdentity.voicePitch);

            // InternIdentity
            return new InternIdentity(idIdentity, name, suitID, voice);
        }

        public int GetRandomAliveIdentityIndex(InternIdentity[] usedIdentities)
        {
            InternIdentity[] allIdentities = new InternIdentity[InternIdentities.Length];
            InternIdentities.CopyTo(allIdentities, 0);
            InternIdentity[] remainingIdentities = allIdentities
                                                    .Except(usedIdentities)
                                                    .Where(x => x.Alive)
                                                    .ToArray();

            if (remainingIdentities.Length == 0)
            {
                return -1;
            }

            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, remainingIdentities.Length);
            return remainingIdentities[randomIndex].IdIdentity;
        }

        public int GetNextAliveIdentityIndex(InternIdentity[] usedIdentities)
        {
            InternIdentity[] allIdentities = new InternIdentity[InternIdentities.Length];
            InternIdentities.CopyTo(allIdentities, 0);
            InternIdentity[] remainingIdentities = allIdentities
                                                    .Except(usedIdentities)
                                                    .Where(x => x.Alive)
                                                    .ToArray();

            if (remainingIdentities.Length == 0)
            {
                return -1;
            }

            return remainingIdentities[0].IdIdentity;
        }

        public int[] GetSelectedIdentitiesToDropAlive()
        {
            if (InternIdentities == null)
            {
                return new int[0];
            }

            return InternIdentities
                        .Where(x => x.Alive && x.SelectedToDrop)
                        .Select(x => x.IdIdentity)
                        .ToArray();
        }

        public string[] GetIdentitiesNamesLowerCaseWithoutSpace()
        {
            if (InternIdentities == null)
            {
                return new string[0];
            }

            return InternIdentities
                        .Select(x => string.Join(' ', x.Name).ToLowerInvariant())
                        .ToArray();
        }
    }
}

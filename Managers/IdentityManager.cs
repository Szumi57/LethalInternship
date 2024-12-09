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

        public void InitIdentities(int nbIdentities, ConfigIdentity[] configIdentities)
        {
            Plugin.LogDebug($"InitIdentities, nbIdentities {nbIdentities}");
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
                configIdentity = ConfigConst.DEFAULT_CONFIG_IDENTITY;
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

        public int GetNewIdentityToSpawn()
        {
            // Get identity
            if (Plugin.Config.SpawnIdentitiesRandomly)
            {
                return GetRandomAvailableAliveIdentityIndex();
            }
            else
            {
                return GetNextAvailableAliveIdentityIndex();
            }
        }

        public int GetRandomAvailableAliveIdentityIndex()
        {
            InternIdentity[] availableIdentities = InternIdentities.FilterAvailableAlive().ToArray();
            if (availableIdentities.Length == 0)
            {
                return -1;
            }

            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, availableIdentities.Length);
            return availableIdentities[randomIndex].IdIdentity;
        }

        public int GetNextAvailableAliveIdentityIndex()
        {
            InternIdentity[] availableIdentities = InternIdentities.FilterAvailableAlive().ToArray();
            if (availableIdentities.Length == 0)
            {
                return -1;
            }

            return availableIdentities[0].IdIdentity;
        }

        public int GetNbIdentitiesAvailable()
        {
            return InternIdentities.FilterAvailableAlive().Count();
        }

        public int GetNbIdentitiesToDrop()
        {
            return InternIdentities.FilterToDropAlive().Count();
        }

        public int[] GetIdentitiesToDrop()
        {
            if (InternIdentities == null)
            {
                return new int[0];
            }

            return InternIdentities
                        .FilterToDropAlive()
                        .Select(x => x.IdIdentity)
                        .ToArray();
        }

        public bool IsAnIdentityToDrop()
        {
            return InternIdentities.FilterToDropAlive().Any();
        }

        public int GetNbIdentitiesSpawned()
        {
            return InternIdentities.FilterSpawnedAlive().Count();
        }
    }

    internal static class IdentityEnumerableExtension
    {
        public static IEnumerable<InternIdentity> FilterAvailableAlive(this IEnumerable<InternIdentity> enumerable)
        {
            return enumerable.Where(x => x.Status == EnumStatusIdentity.Available && x.Alive);
        }

        public static IEnumerable<InternIdentity> FilterToDropAlive(this IEnumerable<InternIdentity> enumerable)
        {
            return enumerable.Where(x => x.Status == EnumStatusIdentity.ToDrop && x.Alive);
        }

        public static IEnumerable<InternIdentity> FilterSpawnedAlive(this IEnumerable<InternIdentity> enumerable)
        {
            return enumerable.Where(x => x.Status == EnumStatusIdentity.Spawned && x.Alive);
        }
    }
}

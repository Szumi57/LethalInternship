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
    public class IdentityManager : MonoBehaviour
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
                    internIdentity.Voice.CountTime(Time.deltaTime);
                }
            }
        }

        public void InitIdentities(ConfigIdentity[] configIdentities)
        {
            Plugin.LogDebug($"InitIdentities, nbIdentities {configIdentities.Length}");
            InternIdentities = new InternIdentity[configIdentities.Length];
            this.configIdentities = configIdentities;

            // InitNewIdentity
            for (int i = 0; i < configIdentities.Length; i++)
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
                configIdentity.voicePitch = UnityEngine.Random.Range(0.8f, 1.2f);
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
                Plugin.LogWarning($"Could not get option for intern suit config in config file, value {configIdentity.suitConfigOption} for {configIdentity.name}, now using random suit.");
                suitConfig = EnumOptionSuitConfig.Random;
            }
            else
            {
                suitConfig = (EnumOptionSuitConfig)configIdentity.suitConfigOption;
            }

            switch (suitConfig)
            {
                case EnumOptionSuitConfig.Fixed:
                    suitID = configIdentity.suitID;
                    break;

                case EnumOptionSuitConfig.Random:
                    suitID = null;
                    break;
            }

            // Voice
            InternVoice voice = new InternVoice(configIdentity.voiceFolder,
                                                configIdentity.volume, 
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
            int idNewIdentity;
            if (Plugin.Config.SpawnIdentitiesRandomly)
            {
                idNewIdentity = GetRandomAvailableAliveIdentityIndex();
            }
            else
            {
                idNewIdentity = GetNextAvailableAliveIdentityIndex();
            }

            // No more identities
            // Create new ones
            if (idNewIdentity == -1)
            {
                ExpandWithNewDefaultIdentities(numberToAdd: 1);
                return InternIdentities.Length - 1;
            }

            return idNewIdentity;
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

        public void ExpandWithNewDefaultIdentities(int numberToAdd)
        {
            Array.Resize(ref InternIdentities, InternIdentities.Length + numberToAdd);
            for (int i = InternIdentities.Length - numberToAdd; i < InternIdentities.Length; i++)
            {
                InternIdentities[i] = InitNewIdentity(i);
            }
        }

        public InternIdentity? FindIdentityFromBodyName(string bodyName)
        {
            string name = bodyName.Replace("Body of ", "");
            return InternIdentities.FirstOrDefault(x => x.Name == name);
        }

        public int GetNbIdentitiesAvailable()
        {
            return Plugin.Config.MaxInternsAvailable - InternIdentities.FilterToDropOrSpawnedAlive().Count();
        }

        public int GetNbIdentitiesToDrop()
        {
            return InternIdentities.FilterToDropAlive().Count();
        }

        public int GetNbIddentitiesToDropOrSpawned()
        {
            return InternIdentities.FilterToDropOrSpawnedAlive().Count();
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

        public static IEnumerable<InternIdentity> FilterToDropOrSpawnedAlive(this IEnumerable<InternIdentity> enumerable)
        {
            return enumerable.Where(x => (x.Status == EnumStatusIdentity.ToDrop || x.Status == EnumStatusIdentity.Spawned)
                                         && x.Alive);
        }

        public static IEnumerable<InternIdentity> FilterSpawnedAlive(this IEnumerable<InternIdentity> enumerable)
        {
            return enumerable.Where(x => x.Status == EnumStatusIdentity.Spawned && x.Alive);
        }
    }
}

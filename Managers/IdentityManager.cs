using HarmonyLib;
using LethalInternship.AI;
using LethalInternship.Configs;
using LethalInternship.Enums;
using LethalInternship.VoiceAdapter;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace LethalInternship.Managers
{
    internal class IdentityManager : MonoBehaviour
    {
        public static IdentityManager Instance { get; private set; } = null!;

        // Names
        private string[] arrayOfNames = null!;

        private void Awake()
        {
            Instance = this;
            Plugin.LogDebug("=============== awake IdentityManager =====================");

            arrayOfNames = GetArrayOfNames();
        }

        public InternIdentity InitNewIdentity(int indexIntern)
        {
            // Get a config identity
            string name;
            ConfigIdentity configIdentity;
            if (indexIntern >= Plugin.Config.ConfigIdentities.configIdentities.Length)
            {
                configIdentity = Const.DEFAULT_CONFIG_IDENTITY;
                name = string.Format(configIdentity.name, indexIntern);
            }
            else
            {
                configIdentity = Plugin.Config.ConfigIdentities.configIdentities[indexIntern];
                name = configIdentity.name;
            }

            // Suit
            int suitID = 0;
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
                    suitID = GetRandomSuitID();
                    break;

            }

            // Voice
            InternVoice voice = new InternVoice();

            // InternIdentity
            return new InternIdentity(indexIntern, name, suitID, voice);
        }

        private string[] GetArrayOfNames()
        {
            EnumOptionNames enumOptionInternNames = Plugin.Config.GetOptionInternNames();

            switch (enumOptionInternNames)
            {
                case EnumOptionNames.DefaultCustomList:
                    return Const.DEFAULT_LIST_CUSTOM_INTERN_NAMES;

                case EnumOptionNames.UserCustomList:

                    if (string.IsNullOrWhiteSpace(Plugin.Config.ListUserCustomNames.Value))
                    {
                        return new string[0];
                    }

                    string[] arrayOfNames = Plugin.Config.ListUserCustomNames.Value.Split(new[] { ',', ';' });
                    for (int i = 0; i < arrayOfNames.Length; i++)
                    {
                        arrayOfNames[i] = arrayOfNames[i].Trim();
                    }

                    // Add the default of names at the end
                    arrayOfNames.AddRangeToArray(Const.DEFAULT_LIST_CUSTOM_INTERN_NAMES);

                    return arrayOfNames;

                default:
                    return new string[0];
            }
        }

        private string GetName(int indexIntern)
        {
            if (indexIntern >= arrayOfNames.Length)
            {
                return string.Format(Const.DEFAULT_INTERN_NAME, indexIntern);
            }

            return arrayOfNames[indexIntern];
        }

        private int GetRandomSuitID()
        {
            StartOfRound instanceSOR = StartOfRound.Instance;
            UnlockableItem unlockableItem;
            List<int> indexesSpawnedUnlockables = new List<int>();
            foreach (var unlockable in instanceSOR.SpawnedShipUnlockables)
            {
                if (unlockable.Value == null)
                {
                    continue;
                }

                unlockableItem = instanceSOR.unlockablesList.unlockables[unlockable.Key];
                if (unlockableItem != null
                    && unlockableItem.unlockableType == 0)
                {
                    // Suits
                    indexesSpawnedUnlockables.Add(unlockable.Key);
                    Plugin.LogDebug($"unlockable index {unlockable.Key}");
                }
            }

            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, indexesSpawnedUnlockables.Count);
            Plugin.LogDebug($"randomIndex {randomIndex}, random suit id {indexesSpawnedUnlockables[randomIndex]}");
            return indexesSpawnedUnlockables[randomIndex];
        }
    }
}

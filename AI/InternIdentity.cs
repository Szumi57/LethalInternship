﻿using System.Collections.Generic;
using Random = System.Random;

namespace LethalInternship.AI
{
    internal class InternIdentity
    {
        public int IdIdentity { get; }
        public string Name { get; set; }
        public InternVoice Voice { get; set; }
        public int? SuitID { get; set; }

        public InternIdentity(int idIdentity, string name, int? suitID, InternVoice voice)
        {
            IdIdentity = idIdentity;
            Name = name;
            SuitID = suitID;
            Voice = voice;
        }

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, name: {Name}, suitID {SuitID}, Voice : {{{Voice.ToString()}}}";
        }

        public int GetRandomSuitID()
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

            Plugin.LogDebug($"indexesSpawnedUnlockables.Count {indexesSpawnedUnlockables.Count}");

            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, indexesSpawnedUnlockables.Count);
            Plugin.LogDebug($"randomIndex {randomIndex}, random suit id {indexesSpawnedUnlockables[randomIndex]}");
            return indexesSpawnedUnlockables[randomIndex];
        }
    }
}

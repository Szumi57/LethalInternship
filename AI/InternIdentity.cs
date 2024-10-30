using System.Collections.Generic;
using Random = System.Random;

namespace LethalInternship.AI
{
    internal class InternIdentity
    {
        public int IdIdentity { get; }
        public string Name { get; set; }
        public InternVoice Voice { get; set; }

        private int? _suitID;
        public int SuitID
        {
            get
            {
                if (_suitID.HasValue)
                {
                    return _suitID.Value;
                }
                else
                {
                    _suitID = GetRandomSuitID();
                    return _suitID.Value;
                }
            }
        }


        public InternIdentity(int idIdentity, string name, int? suitID, InternVoice voice)
        {
            IdIdentity = idIdentity;
            Name = name;
            _suitID = suitID;
            Voice = voice;
        }

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, name: {Name}, suitID {_suitID}";
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

            Plugin.LogDebug($"indexesSpawnedUnlockables.Count {indexesSpawnedUnlockables.Count}");

            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, indexesSpawnedUnlockables.Count);
            Plugin.LogDebug($"randomIndex {randomIndex}, random suit id {indexesSpawnedUnlockables[randomIndex]}");
            return indexesSpawnedUnlockables[randomIndex];
        }
    }
}

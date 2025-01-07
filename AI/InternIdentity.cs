using LethalInternship.Enums;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace LethalInternship.AI
{
    internal class InternIdentity
    {
        public int IdIdentity { get; }
        public string Name { get; set; }
        public int? SuitID { get; set; }
        public InternVoice Voice { get; set; }
        public DeadBodyInfo? DeadBody { get; set; }
        public object? BodyReplacementBase { get; set; }

        public int HpMax { get; set; }
        public int Hp { get; set; }
        public EnumStatusIdentity Status { get; set; }

        public bool Alive { get { return Hp > 0; } }
        public string Suit
        {
            get
            {
                if (!SuitID.HasValue)
                {
                    return "";
                }

                string suitName = SuitID.Value > StartOfRound.Instance.unlockablesList.unlockables.Count() ? "Not found" : StartOfRound.Instance.unlockablesList.unlockables[SuitID.Value].unlockableName;
                return $"{SuitID.Value}: {suitName}";
            }
        }

        public InternIdentity(int idIdentity, string name, int? suitID, InternVoice voice)
        {
            IdIdentity = idIdentity;
            Name = name;
            SuitID = suitID;
            Voice = voice;
            HpMax = Plugin.Config.InternMaxHealth.Value;
            Hp = HpMax;
            Status = EnumStatusIdentity.Available;
        }

        public void UpdateIdentity(int Hp, int? suitID, EnumStatusIdentity enumStatusIdentity)
        {
            this.Hp = Hp;
            this.SuitID = suitID;
            this.Status = enumStatusIdentity;
        }

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, name: {Name}, suit {Suit}, Hp {Hp}/{HpMax}, Status {(int)Status} '{Status}', Voice : {{{Voice.ToString()}}}";
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
                    //Plugin.LogDebug($"unlockable index {unlockable.Key}");
                }
            }

            if (indexesSpawnedUnlockables.Count == 0)
            {
                return 0;
            }

            //Plugin.LogDebug($"indexesSpawnedUnlockables.Count {indexesSpawnedUnlockables.Count}");
            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, indexesSpawnedUnlockables.Count);
            if (randomIndex >= indexesSpawnedUnlockables.Count)
            {
                return 0;
            }

            return indexesSpawnedUnlockables[randomIndex];
        }
    }
}

using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace LethalInternship.Core.Interns
{
    public class InternIdentity : IInternIdentity
    {
        public int IdIdentity => idIdentity;
        public string Name => name;
        public int Hp { get => hp; set => hp = value; }
        public int HpMax { get => hpMax; set => hpMax = value; }
        public int? SuitID { get => suitID; set => suitID = value; }
        public DeadBodyInfo? DeadBody { get => deadBody; set => deadBody = value; }
        public EnumStatusIdentity Status { get => status; set => status = value; }

        public IInternVoice Voice => voice;
        public object? BodyReplacementBase { get => bodyReplacementBase; set => bodyReplacementBase = value; }
        public bool Alive { get { return Hp > 0; } }

        private int idIdentity;
        private string name;
        private int hp;
        private int hpMax;
        private int? suitID;
        private DeadBodyInfo? deadBody;
        public EnumStatusIdentity status;
        private IInternVoice voice;
        private object? bodyReplacementBase;

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
            this.idIdentity = idIdentity;
            this.name = name;
            this.suitID = suitID;
            this.voice = voice;
            this.hpMax = PluginRuntimeProvider.Context.Config.InternMaxHealth;
            this.Hp = hpMax;
            this.status = EnumStatusIdentity.Available;
        }

        public void UpdateIdentity(int Hp, int? suitID, EnumStatusIdentity enumStatusIdentity)
        {
            this.Hp = Hp;
            this.suitID = suitID;
            this.status = enumStatusIdentity;
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
                    //PluginLoggerHook.LogDebug?.Invoke($"unlockable index {unlockable.Key}");
                }
            }

            if (indexesSpawnedUnlockables.Count == 0)
            {
                return 0;
            }

            //PluginLoggerHook.LogDebug?.Invoke($"indexesSpawnedUnlockables.Count {indexesSpawnedUnlockables.Count}");
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

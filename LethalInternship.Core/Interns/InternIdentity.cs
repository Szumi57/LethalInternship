using LethalInternship.Core.Managers;
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
        public IInternAI? InternAI { get => internAI; set => internAI = value; }
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

        public int[] ItemsInInventory => itemsInInventory;

        private int idIdentity;

        private IInternAI? internAI;
        private string name;

        private int hp;
        private int hpMax;

        private int? suitID;
        private DeadBodyInfo? deadBody;
        public EnumStatusIdentity status;
        private IInternVoice voice;
        private object? bodyReplacementBase;

        private int[] itemsInInventory;

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
            this.itemsInInventory = new int[0];
        }

        public void UpdateIdentity(int Hp,
                                   int? suitID,
                                   EnumStatusIdentity enumStatusIdentity,
                                   int[]? itemsInInventory)
        {
            this.Hp = Hp;
            this.suitID = suitID;
            this.status = enumStatusIdentity;
            if (itemsInInventory != null)
            {
                this.itemsInInventory = itemsInInventory;
            }
        }

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, name: {Name}, suit {Suit}, Hp {Hp}/{HpMax}, Status {(int)Status} '{Status}', Voice : {{{Voice.ToString()}}}, Items : {string.Join(",", itemsInInventory)}";
        }

        public int GetRandomSuitID()
        {
            List<int> indexesSpawnedSuits = InternManager.Instance.GetListOfAvailableSuitIDs();
            if (indexesSpawnedSuits.Count == 0)
            {
                return 0;
            }

            //PluginLoggerHook.LogDebug?.Invoke($"indexesSpawnedSuits.Count {indexesSpawnedSuits.Count}");
            Random randomInstance = new Random();
            int randomIndex = randomInstance.Next(0, indexesSpawnedSuits.Count);
            if (randomIndex >= indexesSpawnedSuits.Count)
            {
                return 0;
            }

            return indexesSpawnedSuits[randomIndex];
        }

        public void UpdateItemsInInventory(int[] itemsID)
        {
            itemsInInventory = itemsID;
        }
    }
}

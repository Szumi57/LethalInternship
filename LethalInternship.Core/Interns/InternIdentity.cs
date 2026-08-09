using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
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

        private Action<IInternIdentity> onAutoDefenseChanged = null!;
        public Action<IInternIdentity>? OnAutoDefenseChanged { get { return onAutoDefenseChanged; } set { onAutoDefenseChanged = value!; } }
        public bool AutoDefense { get; private set; }

        private Action<IInternIdentity> onCommandChanged = null!;
        public Action<IInternIdentity>? OnCommandChanged { get { return onCommandChanged; } set { onCommandChanged = value!; } }

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
        private List<int> _indexesSpawnedSuits = new List<int>();
        private int _currentSuitIndex = -1;

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


        public InternIdentity(int idIdentity, string name, int? suitID, bool autoDefense, InternVoice voice)
        {
            this.idIdentity = idIdentity;
            this.name = name;
            this.suitID = suitID;
            this.voice = voice;
            this.hpMax = PluginRuntimeProvider.Context.Config.InternMaxHealth;
            this.Hp = hpMax;
            this.status = EnumStatusIdentity.Available;
            this.itemsInInventory = new int[0];
            this.AutoDefense = autoDefense;
        }

        public void UpdateIdentity(int Hp,
                                   int? suitID,
                                   EnumStatusIdentity enumStatusIdentity,
                                   int[]? itemsInInventory,
                                   bool autoDefense)
        {
            this.Hp = Hp;
            this.suitID = suitID;
            this.status = enumStatusIdentity;
            if (itemsInInventory != null)
            {
                this.itemsInInventory = itemsInInventory;
            }
            this.AutoDefense = autoDefense;
        }

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, name: {Name}, suit {Suit}, Hp {Hp}/{HpMax}, Status {(int)Status} '{Status}', " +
                   $"Voice : {{{Voice.ToString()}}}, Items : {string.Join(",", itemsInInventory)}, AutoDefense: {AutoDefense}";
        }

        #region Suits

        private void RefreshListSuits()
        {
            _indexesSpawnedSuits = InternManager.Instance.GetListOfAvailableSuitIDs();
        }

        public int GetPreviousSuitID()
        {
            RefreshListSuits();
            if (_indexesSpawnedSuits.Count == 0) return 0;

            _currentSuitIndex--;

            if (_currentSuitIndex < 0)
                _currentSuitIndex = _indexesSpawnedSuits.Count - 1;

            if (_currentSuitIndex >= _indexesSpawnedSuits.Count)
                _currentSuitIndex = 0;

            return _indexesSpawnedSuits[_currentSuitIndex];
        }

        public int GetNextSuitID()
        {
            RefreshListSuits();
            if (_indexesSpawnedSuits.Count == 0) return 0;

            _currentSuitIndex = (_currentSuitIndex + 1) % _indexesSpawnedSuits.Count;

            if (_currentSuitIndex < 0)
                _currentSuitIndex = _indexesSpawnedSuits.Count - 1;

            if (_currentSuitIndex >= _indexesSpawnedSuits.Count)
                _currentSuitIndex = 0;

            return _indexesSpawnedSuits[_currentSuitIndex];
        }

        public int GetRandomSuitID()
        {
            RefreshListSuits();
            if (_indexesSpawnedSuits.Count == 0)
            {
                return 0;
            }
            if (_indexesSpawnedSuits.Count == 1)
            {
                _currentSuitIndex = 0;
                return _indexesSpawnedSuits[_currentSuitIndex];
            }

            Random randomInstance = new Random();
            int randomIndex;
            do
            {
                randomIndex = randomInstance.Next(0, _indexesSpawnedSuits.Count);
                //Debug.Log($"indexesSpawnedSuits.Count {_indexesSpawnedSuits.Count} randomIndex {randomIndex}");
                if (randomIndex >= _indexesSpawnedSuits.Count)
                    return 0;
            }
            while (_indexesSpawnedSuits[randomIndex] == SuitID);

            _currentSuitIndex = randomIndex;
            return _indexesSpawnedSuits[_currentSuitIndex];
        }

        #endregion

        public void UpdateItemsInInventory(int[] itemsID)
        {
            itemsInInventory = itemsID;
        }

        public void SetAutoDefense(bool autoDefense)
        {
            AutoDefense = autoDefense;
            OnAutoDefenseChanged?.Invoke(this);
        }
    }
}

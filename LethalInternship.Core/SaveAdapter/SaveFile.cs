using LethalInternship.SharedAbstractions.Enums;
using System;
using System.Linq;

namespace LethalInternship.Core.SaveAdapter
{
    /// <summary>
    /// Represents the data serializable, to be saved on disk, necessay for LethalInternship
    /// </summary>
    [Serializable]
    internal class SaveFile
    {
        public bool LandingStatusAborted;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IdentitySaveFile[] IdentitiesSaveFiles;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

    [Serializable]
    internal class IdentitySaveFile
    {
        public int IdIdentity;
        public int SuitID;
        public int Hp;
        public int Status;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Inventory Inventory;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public override string ToString()
        {
            return $"IdIdentity: {IdIdentity}, suitID {SuitID}, Hp {Hp}, Status {Status} {(EnumStatusIdentity)Status}";
        }
    }

    [Serializable]
    internal class Inventory
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Item[] Items;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public Inventory(int[] itemIds)
        {
            if (itemIds == null)
            {
                Items = new Item[0];
            }
            else
            {
                Items = new Item[itemIds.Length];
                for (int i = 0; i < itemIds.Length; i++)
                {
                    Items[i] = new Item(itemIds[i]);
                }
            }
        }

        public int[] GetItemIDs()
        {
            return Items.Select(x => x.ID).ToArray();
        }

        public override string ToString()
        {
            return string.Concat("Items :", string.Join("\r\n                                                               ", Items.ToString()));
        }
    }

    [Serializable]
    internal class Item
    {
        public int ID;

        public Item(int iD)
        {
            ID = iD;
        }

        public override string ToString()
        {
            return $"ID : {ID}";
        }
    }
}

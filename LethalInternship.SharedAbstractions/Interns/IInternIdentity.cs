using LethalInternship.SharedAbstractions.Enums;
using System;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInternIdentity
    {
        IInternAI? InternAI { get; set; }
        int IdIdentity { get; }
        string Name { get; }
        int Hp { get; set; }
        int HpMax { get; set; }
        int? SuitID { get; set; }
        DeadBodyInfo? DeadBody { get; set; }
        EnumStatusIdentity Status { get; set; }
        IInternVoice Voice { get; }
        object? BodyReplacementBase { get; set; }
        bool Alive { get; }
        int[] ItemsInInventory { get; }

        Action<IInternIdentity>? OnAutoDefenseChanged { get; set; }
        bool AutoDefense { get; }

        int GetPreviousSuitID();
        int GetNextSuitID();
        int GetRandomSuitID();


        void UpdateIdentity(int Hp, int? suitID, EnumStatusIdentity enumStatusIdentity, int[]? itemsInInventory, bool autoDefense);

        void UpdateItemsInInventory(int[] itemsID);

        void SetAutoDefense(bool autoDefense);
    }
}

using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInternIdentity
    {
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

        int GetRandomSuitID();
        void UpdateIdentity(int Hp, int? suitID, EnumStatusIdentity enumStatusIdentity, int[]? itemsInInventory);

        void UpdateItemsInInventory(int[] itemsID);
    }
}

namespace LethalInternship.SharedAbstractions.Enums
{
    [System.Flags]
    public enum EnumIconImagesTypes
    {
        None = 0,
        Attack = 1 << 0,
        Default = 1 << 1,
        GatheringPoint = 1 << 2,
        Position = 1 << 3,
        Ship = 1 << 4,
        Vehicle = 1 << 5,
    }
}

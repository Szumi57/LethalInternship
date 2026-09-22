using LethalInternship.SharedAbstractions.Enums;

namespace LethalInternship.SharedAbstractions.UI
{
    public interface IIconUIInfos
    {
        EnumIconImagesTypes IconImagesTypes { get; }

        int GetUIKey();
    }
}

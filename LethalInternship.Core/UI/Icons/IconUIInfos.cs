using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.UI;

namespace LethalInternship.Core.UI.Icons
{
    public class IconUIInfos : IIconUIInfos
    {
        public EnumIconImagesTypes IconImagesTypes => iconImagesTypes;

        private int UIKey;
        private EnumIconImagesTypes iconImagesTypes;

        public IconUIInfos(EnumIconImagesTypes iconImagesTypes)
        {
            this.iconImagesTypes = iconImagesTypes;
            UIKey = (int)iconImagesTypes;
        }

        public int GetUIKey()
        {
            return UIKey;
        }
    }
}

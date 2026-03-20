using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.UI;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Icons
{
    public class IconUIInfos : IIconUIInfos
    {
        public EnumIconImagesTypes IconImagesTypes => iconImagesTypes;

        private string UIKey;
        private List<GameObject> imagesPrefabs;
        private EnumIconImagesTypes iconImagesTypes;

        public IconUIInfos(string uIKey, List<GameObject> imagesPrefabs, EnumIconImagesTypes iconImagesTypes)
        {
            this.iconImagesTypes = iconImagesTypes;
            this.imagesPrefabs = imagesPrefabs;
            UIKey = uIKey;
        }

        public List<GameObject> GetImagesPrefab()
        {
            return imagesPrefabs;
        }

        public string GetUIKey()
        {
            return UIKey;
        }
    }
}

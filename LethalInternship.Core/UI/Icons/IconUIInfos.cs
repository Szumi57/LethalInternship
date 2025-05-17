using LethalInternship.SharedAbstractions.UI;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Icons
{
    public class IconUIInfos : IIconUIInfos
    {
        private string UIKey;
        private List<GameObject> imagesPrefabs;

        public IconUIInfos(string uIKey, List<GameObject> imagesPrefabs)
        {
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

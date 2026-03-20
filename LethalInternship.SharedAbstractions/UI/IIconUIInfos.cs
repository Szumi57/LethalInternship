using LethalInternship.SharedAbstractions.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.UI
{
    public interface IIconUIInfos
    {
        EnumIconImagesTypes IconImagesTypes { get; }

        string GetUIKey();
        List<GameObject> GetImagesPrefab();
    }
}

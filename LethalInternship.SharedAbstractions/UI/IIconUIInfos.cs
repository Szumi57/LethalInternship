using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.UI
{
    public interface IIconUIInfos
    {
        string GetUIKey();

        List<GameObject> GetImagesPrefab();
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Interfaces
{
    public interface IIconUIInfos
    {
        string GetUIKey();

        List<GameObject> GetImagesPrefab();
    }
}

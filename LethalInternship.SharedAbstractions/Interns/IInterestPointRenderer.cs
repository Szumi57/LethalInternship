using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInterestPointRenderer<in T> where T : IInterestPoint
    {
        EnumIconImagesTypes GetIconImagesTypes(T interestPoint);
        GameObject GetImagePrefab(T interestPoint);
        Vector3 GetUIPos(T interestPoint);
    }
}

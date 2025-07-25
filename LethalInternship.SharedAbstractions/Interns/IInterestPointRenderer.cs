using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInterestPointRenderer<in T> where T : IInterestPoint
    {
        GameObject GetImagePrefab(T interestPoint);
        Vector3 GetUIPos(T interestPoint);
    }
}

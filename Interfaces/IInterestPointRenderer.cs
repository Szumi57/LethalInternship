using UnityEngine;

namespace LethalInternship.Interfaces
{
    public interface IInterestPointRenderer<in T> where T : IInterestPoint
    {
        GameObject GetImagePrefab(T interestPoint);
    }
}

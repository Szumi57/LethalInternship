using UnityEngine;

namespace LethalInternship.Interfaces
{
    public interface IInterestPointRendererWrapper
    {
        GameObject GetImagePrefab(IInterestPoint interestPoint);
    }
}

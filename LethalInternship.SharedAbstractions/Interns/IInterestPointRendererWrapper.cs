using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInterestPointRendererWrapper
    {
        GameObject GetImagePrefab(IInterestPoint interestPoint);
        Vector3 GetUIPos(IInterestPoint interestPoint);
    }
}

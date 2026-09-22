using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInterestPointRendererWrapper
    {
        EnumIconImagesTypes GetIconImagesTypes(IInterestPoint interestPoint);
        Vector3 GetUIPos(IInterestPoint interestPoint);
    }
}

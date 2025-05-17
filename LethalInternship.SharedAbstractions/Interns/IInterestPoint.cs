using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInterestPoint
    {
        Vector3 Point { get; }

        bool IsCompatibleWith(IInterestPoint other);
    }
}

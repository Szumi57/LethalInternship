using UnityEngine;

namespace LethalInternship.Interfaces
{
    public interface IInterestPoint
    {
        Vector3 Point { get; }

        bool IsCompatibleWith(IInterestPoint other);
    }
}

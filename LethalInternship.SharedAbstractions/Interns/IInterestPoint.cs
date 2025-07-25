using LethalInternship.SharedAbstractions.Enums;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Interns
{
    public interface IInterestPoint
    {
        Vector3 Point { get; }
        EnumCommandTypes? CommandType { get; }
        bool IsInvalid { get; }

        bool IsCompatibleWith(IInterestPoint other);
    }
}

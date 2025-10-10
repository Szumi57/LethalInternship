using System;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Adapters
{
    public interface IBodyReplacementBase
    {
        Component BodyReplacementBase { get; }

        Type TypeReplacement { get; }
        bool IsActive { get; set; }
        GameObject? DeadBody { get; }
        string SuitName { get; set; }
    }
}

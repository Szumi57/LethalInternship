using System;

namespace LethalInternship.SharedAbstractions.Pools
{
    public interface IObjectPool
    {
        int Created { get; }
        int Available { get; }
        Type ObjectType { get; }
    }
}

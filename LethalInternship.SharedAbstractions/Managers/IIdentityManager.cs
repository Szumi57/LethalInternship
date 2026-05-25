using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IIdentityManager
    {
        int GetNewIdentityToSpawn();

        int[] GetIdentitiesIDsSpawned();
        List<IInternIdentity> GetIdentitiesSpawned();

        int[] GetIdentitiesToDrop();

        IInternIdentity? FindIdentityFromBodyName(string bodyName);

        bool IsIdentityValidToCommand(IInternIdentity identity);
    }
}

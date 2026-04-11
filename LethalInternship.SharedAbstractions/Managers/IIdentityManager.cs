using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IIdentityManager
    {
        int GetNewIdentityToSpawn();

        int[] GetIdentitiesIDsSpawned();
        IInternIdentity[] GetIdentitiesSpawned();

        int[] GetIdentitiesToDrop();

        IInternIdentity[] GetIdentitiesOwnedByLocal();

        IInternIdentity? FindIdentityFromBodyName(string bodyName);

        bool IsIdentityValidToCommand(IInternIdentity identity);
    }
}

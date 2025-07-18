using LethalInternship.SharedAbstractions.Interns;

namespace LethalInternship.SharedAbstractions.Managers
{
    public interface IIdentityManager
    {
        int GetNewIdentityToSpawn();
        int[] GetIdentitiesSpawned();
        int[] GetIdentitiesToDrop();

        IInternIdentity? FindIdentityFromBodyName(string bodyName);
    }
}

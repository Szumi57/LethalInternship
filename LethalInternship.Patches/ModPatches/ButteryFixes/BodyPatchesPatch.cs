using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.ModPatches.ButteryFixes
{
    public class BodyPatchesPatch
    {
        public static bool DeadBodyInfoPostStart_Prefix(DeadBodyInfo __0)
        {
            if (__0.playerScript != null
                && InternManagerProvider.Instance.IsPlayerIntern(__0.playerScript))
            {
                return false;
            }
            return true;
        }
    }
}

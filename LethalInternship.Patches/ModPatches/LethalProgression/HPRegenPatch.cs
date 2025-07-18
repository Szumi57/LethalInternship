using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.ModPatches.LethalProgression
{
    public class HPRegenPatch
    {
        public static bool HPRegenUpdate_Prefix(PlayerControllerB __0)
        {
            if (InternManagerProvider.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }
    }
}

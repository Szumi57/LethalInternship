using GameNetcodeStuff;
using LethalInternship.Managers;

namespace LethalInternship.Patches.ModPatches.LethalProgression
{
    internal class HPRegenPatch
    {
        public static bool HPRegenUpdate_Prefix(PlayerControllerB __0)
        {
            if (InternManager.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }
    }
}

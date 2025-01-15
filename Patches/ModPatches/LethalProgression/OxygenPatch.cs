using GameNetcodeStuff;
using LethalInternship.Managers;

namespace LethalInternship.Patches.ModPatches.LethalProgression
{
    public class OxygenPatch
    {
        public static bool EnteredWater_Prefix(PlayerControllerB __0)
        {
            if (InternManager.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }

        public static bool LeftWater_Prefix(PlayerControllerB __0)
        {
            if (InternManager.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }

        public static bool ShouldDrown_Prefix(PlayerControllerB __0)
        {
            if (InternManager.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }

        public static bool OxygenUpdate_Prefix(PlayerControllerB __0)
        {
            if (InternManager.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }
    }
}

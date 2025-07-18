using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.ManagerProviders;

namespace LethalInternship.Patches.ModPatches.ReservedItemSlotCore
{
    public class PlayerPatcherPatch
    {
        public static bool InitializePlayerControllerLate_Prefix(PlayerControllerB __0)
        {
            if (InternManagerProvider.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }

        public static bool CheckForChangedInventorySize_Prefix(PlayerControllerB __0)
        {
            if (InternManagerProvider.Instance.IsPlayerIntern(__0))
            {
                return false;
            }

            return true;
        }
    }
}

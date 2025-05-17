using BunkbedRevive;
using LethalInternship.SharedAbstractions.Hooks.BunkbedReviveHooks;

namespace LethalInternship.Patches.ModPatches.BunkbedRevive
{
    public class BunkbedReviveUtils
    {
        public static void Init()
        {
            BunkbedReviveHook.UpdateReviveCount = UpdateReviveCount;
        }

        public static void UpdateReviveCount(int id)
        {
            BunkbedController.UpdateReviveCount(id);
        }
    }
}

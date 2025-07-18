using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Hooks.MoreCompanyHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using MoreCompany.Cosmetics;

namespace LethalInternship.Patches.ModPatches.MoreCompany
{
    public class MoreCompanyUtils
    {
        public static void Init()
        {
            MoreCompanyHook.RemoveCosmetics = RemoveCosmetics;
        }

        public static void RemoveCosmetics(PlayerControllerB internController)
        {
            CosmeticApplication componentInChildren = internController.gameObject.GetComponentInChildren<CosmeticApplication>();
            if (componentInChildren != null)
            {
                PluginLoggerHook.LogDebug?.Invoke("clear cosmetics");
                componentInChildren.RefreshAllCosmeticPositions();
                componentInChildren.ClearCosmetics();
            }
        }
    }
}

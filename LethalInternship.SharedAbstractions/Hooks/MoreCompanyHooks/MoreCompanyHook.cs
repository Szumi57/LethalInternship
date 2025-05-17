using GameNetcodeStuff;

namespace LethalInternship.SharedAbstractions.Hooks.MoreCompanyHooks
{
    public delegate void RemoveCosmeticsDelegate(PlayerControllerB internController);

    public class MoreCompanyHook
    {
        public static RemoveCosmeticsDelegate? RemoveCosmetics;
    }
}

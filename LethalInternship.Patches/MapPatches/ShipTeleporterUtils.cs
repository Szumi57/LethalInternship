using LethalInternship.SharedAbstractions.Hooks.ShipTeleporterHooks;

namespace LethalInternship.Patches.MapPatches
{
    public class ShipTeleporterUtils
    {
        public static void Init()
        {
            ShipTeleporterHook.SetPlayerTeleporterId_ReversePatch = ShipTeleporterPatch.SetPlayerTeleporterId_ReversePatch;
        }
    }
}

using GameNetcodeStuff;

namespace LethalInternship.SharedAbstractions.Hooks.ShipTeleporterHooks
{
    public delegate void SetPlayerTeleporterId_ReversePatchDelegate(object instance, PlayerControllerB playerScript, int teleporterId);

    public  class ShipTeleporterHook
    {
        public static SetPlayerTeleporterId_ReversePatchDelegate? SetPlayerTeleporterId_ReversePatch;
    }
}

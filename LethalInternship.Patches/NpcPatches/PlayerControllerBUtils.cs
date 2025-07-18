using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;

namespace LethalInternship.Patches.NpcPatches
{
    public class PlayerControllerBUtils
    {
        public static void Init()
        {
            PlayerControllerBHook.PlayerHitGroundEffects_ReversePatch = PlayerControllerBPatch.PlayerHitGroundEffects_ReversePatch;
            PlayerControllerBHook.PlayJumpAudio_ReversePatch = PlayerControllerBPatch.PlayJumpAudio_ReversePatch;
            PlayerControllerBHook.SyncBodyPositionClientRpc_ReversePatch = PlayerControllerBPatch.SyncBodyPositionClientRpc_ReversePatch;
            PlayerControllerBHook.SetSpecialGrabAnimationBool_ReversePatch = PlayerControllerBPatch.SetSpecialGrabAnimationBool_ReversePatch;
            PlayerControllerBHook.IsInSpecialAnimationClientRpc_ReversePatch = PlayerControllerBPatch.IsInSpecialAnimationClientRpc_ReversePatch;
            PlayerControllerBHook.OnDisable_ReversePatch = PlayerControllerBPatch.OnDisable_ReversePatch;
            PlayerControllerBHook.InteractTriggerUseConditionsMet_ReversePatch = PlayerControllerBPatch.InteractTriggerUseConditionsMet_ReversePatch;
        }
    }
}

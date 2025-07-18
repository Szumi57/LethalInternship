using UnityEngine;

namespace LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks
{
    public delegate void PlayerHitGroundEffects_ReversePatchDelegate(object instance);
    public delegate void PlayJumpAudio_ReversePatchDelegate(object instance);
    public delegate void IsInSpecialAnimationClientRpc_ReversePatchDelegate(object instance, bool specialAnimation, float timed, bool climbingLadder);
    public delegate void SyncBodyPositionClientRpc_ReversePatchDelegate(object instance, Vector3 newBodyPosition);
    public delegate void SetSpecialGrabAnimationBool_ReversePatchDelegate(object instance, bool setTrue, GrabbableObject currentItem);
    public delegate void OnDisable_ReversePatchDelegate(object instance);
    public delegate bool InteractTriggerUseConditionsMet_ReversePatchDelegate(object instance);

    public class PlayerControllerBHook
    {
        public static PlayerHitGroundEffects_ReversePatchDelegate? PlayerHitGroundEffects_ReversePatch;
        public static PlayJumpAudio_ReversePatchDelegate? PlayJumpAudio_ReversePatch;
        public static IsInSpecialAnimationClientRpc_ReversePatchDelegate? IsInSpecialAnimationClientRpc_ReversePatch;
        public static SyncBodyPositionClientRpc_ReversePatchDelegate? SyncBodyPositionClientRpc_ReversePatch;
        public static SetSpecialGrabAnimationBool_ReversePatchDelegate? SetSpecialGrabAnimationBool_ReversePatch;
        public static OnDisable_ReversePatchDelegate? OnDisable_ReversePatch;
        public static InteractTriggerUseConditionsMet_ReversePatchDelegate? InteractTriggerUseConditionsMet_ReversePatch;
    }
}

using GameNetcodeStuff;
using UnityEngine;

namespace LethalInternship.SharedAbstractions.Hooks.MipaHooks
{
    public delegate AudioClip? GetMipaFootstepAudioClipDelegate(PlayerControllerB npcController);
    public delegate float? GetMipaFootstepVolumeScaleDelegate(PlayerControllerB npcController, int animationHashLayers0);

    public static class MipaHook
    {
        public static GetMipaFootstepAudioClipDelegate? GetMipaFootstepAudioClip;
        public static GetMipaFootstepVolumeScaleDelegate? GetMipaFootstepVolumeScale;
    }
}

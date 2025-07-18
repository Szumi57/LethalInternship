using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.MipaHooks;
using Mipa;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.Mipa
{
    public class MipaUtils
    {
        public static void Init()
        {
            MipaHook.GetMipaFootstepAudioClip = GetMipaFootstepAudioClip;
            MipaHook.GetMipaFootstepVolumeScale = GetMipaFootstepVolumeScale;
        }

        public static AudioClip? GetMipaFootstepAudioClip(PlayerControllerB npcController)
        {
            return npcController.GetComponent<MRMIPA_PLAYER_MODEL>()?.GetFootstepClip();
        }

        public static float? GetMipaFootstepVolumeScale(PlayerControllerB npcController, int animationHashLayers0)
        {
            MRMIPA_PLAYER_MODEL component = npcController.GetComponent<MRMIPA_PLAYER_MODEL>();
            if (component == null)
            {
                return null;
            }

            if (animationHashLayers0 == Const.SPRINTING_STATE_HASH)
            {
                return 0.09f;
            }
            return 0.06f;
        }
    }
}

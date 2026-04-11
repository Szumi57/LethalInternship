using GameNetcodeStuff;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Interns;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.Outlines
{
    public static class InternOutlineController
    {
        private class OutlineState
        {
            public bool enabled;
            public float intensity;
            public float rimPower;
            public Color color;
        }

        const float RIM_EPSILON = 0.02f;
        private static readonly Dictionary<ulong, OutlineState> states = new Dictionary<ulong, OutlineState>();

        public static void UpdateOutlines(IEnumerable<IInternAI> interns,
                                          ulong? pointedInternClientId,
                                          bool allowMultiple,
                                          bool forceNoOutlines = false)
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            foreach (IInternAI intern in interns)
            {
                bool shouldOutline;
                if (forceNoOutlines)
                {
                    shouldOutline = false;
                }
                else
                {
                    shouldOutline = !intern.Npc.isPlayerDead
                                    && (intern.Npc.playerClientId == pointedInternClientId || allowMultiple)
                                    && ((intern.OwnerClientId == localPlayer.OwnerClientId && intern.NpcController.GetSqrDistanceWithLocalPlayer() < UIConst.DISTANCE_UI_PROXIMITY * UIConst.DISTANCE_UI_PROXIMITY)
                                        || intern.NpcController.GetSqrDistanceWithLocalPlayer() < localPlayer.grabDistance * localPlayer.grabDistance);
                }

                float distance = intern.NpcController.GetSqrDistanceWithLocalPlayer();
                float t = Mathf.InverseLerp(1f, UIConst.DISTANCE_SOLID_OUTLINE * UIConst.DISTANCE_SOLID_OUTLINE, distance);
                float rimPower = Mathf.Lerp(UIConst.OUTLINE_RIM_DEFAULT,
                                            UIConst.OUTLINE_RIM_SOLID,
                                            t);
                bool owned = StartOfRound.Instance != null
                            && StartOfRound.Instance.localPlayerController != null
                            && StartOfRound.Instance.localPlayerController.actualClientId == intern.OwnerClientId;

                ApplyState(intern,
                           shouldOutline,
                           intensity: UIConst.OUTLINE_INTENSITY_DEFAULT,
                           rimPower,
                           color: owned ? UIConst.UI_COLOR_ORANGE : UIConst.UI_COLOR_DEFAULT);
            }
        }

        private static void ApplyState(IInternAI intern,
                                       bool shouldEnable,
                                       float intensity,
                                       float rimPower,
                                       Color color)
        {
            if (!states.TryGetValue(intern.Npc.playerClientId, out var state))
            {
                state = new OutlineState();
                states[intern.Npc.playerClientId] = state;
            }

            if (shouldEnable != state.enabled)
            {
                if (shouldEnable)
                {
                    SimpleOutline.Add(intern.Npc.gameObject,
                                      color,
                                      rimPower,
                                      intensity);
                }
                else
                {
                    SimpleOutline.Remove(intern.Npc.gameObject);
                }

                state.enabled = shouldEnable;
            }

            if (shouldEnable)
            {
                bool rimChanged = Mathf.Abs(state.rimPower - rimPower) > RIM_EPSILON;
                bool intensityChanged = state.intensity != intensity;
                bool colorChanged = state.color != color;
                if (rimChanged || intensityChanged || colorChanged)
                {
                    SimpleOutline.UpdateParams(intern.Npc.gameObject,
                                               intensity,
                                               rimPower,
                                               color);

                    state.intensity = intensity;
                    state.rimPower = Mathf.Lerp(
                                                state.rimPower,
                                                rimPower,
                                                Time.deltaTime * 10f
                                                );
                    state.color = color;
                }
            }
        }
    }
}

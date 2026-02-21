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

        private static readonly Dictionary<ulong, OutlineState> states =
        new Dictionary<ulong, OutlineState>();

        public static void UpdateOutlines(IInternAI[] interns,
                                          ulong? pointedInternClientId,
                                          bool allowMultiple)
        {
            foreach (IInternAI intern in interns)
            {
                bool shouldOutline = intern.Npc.playerClientId == pointedInternClientId || allowMultiple;

                float distance = intern.NpcController.GetSqrDistanceWithLocalPlayer(intern.Npc.transform.position);
                float rimPower = UIConst.OUTLINE_RIM_DEFAULT;
                if (distance > UIConst.DISTANCE_SOLID_OUTLINE * UIConst.DISTANCE_SOLID_OUTLINE)
                {
                    rimPower = UIConst.OUTLINE_RIM_SOLID;
                }

                ApplyState(intern,
                           shouldOutline,
                           intensity: UIConst.OUTLINE_INTENSITY_DEFAULT,
                           rimPower,
                           color: UIConst.OUTLINE_COLOR_DEFAULT);
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
                if (state.color != color || state.rimPower != rimPower)
                {
                    SimpleOutline.UpdateParams(intern.Npc.gameObject,
                                               intensity,
                                               rimPower,
                                               color);

                    state.intensity = intensity;
                    state.rimPower = rimPower;
                    state.color = color;
                }
            }
        }
    }
}

using GameNetcodeStuff;
using LethalInternship.Core.Managers;
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
        private static readonly Dictionary<ulong, OutlineState> internStates = new Dictionary<ulong, OutlineState>();
        private static readonly Dictionary<EnemyAI, OutlineState> enemiesStates = new Dictionary<EnemyAI, OutlineState>();
        private static readonly Dictionary<GrabbableObject, OutlineState> itemStates = new Dictionary<GrabbableObject, OutlineState>();

        public static void UpdateInternsOutlines(IEnumerable<IInternIdentity> identities,
                                                  ulong? pointedInternClientId,
                                                  bool allowMultiple,
                                                  bool forceNoOutlines = false)
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            foreach (IInternIdentity identity in identities)
            {
                IInternAI? intern = identity.InternAI;
                if (intern == null) continue;

                bool shouldOutline;
                if (forceNoOutlines)
                {
                    shouldOutline = false;
                }
                else
                {
                    shouldOutline = !intern.Npc.isPlayerDead
                                    && !intern.IsSpawningAnimationRunning()
                                    && (intern.Npc.playerClientId == pointedInternClientId || allowMultiple)
                                    && ((intern.OwnerClientId == localPlayer.OwnerClientId && intern.NpcController.GetSqrDistanceWithLocalPlayer() < Const.DISTANCE_COMMAND_PROXIMITY * Const.DISTANCE_COMMAND_PROXIMITY)
                                        || intern.NpcController.GetSqrDistanceWithLocalPlayer() < localPlayer.grabDistance * localPlayer.grabDistance);
                }

                float rimPower = UIConst.OUTLINE_RIM_DEFAULT;
                if (shouldOutline)
                {
                    float distance = intern.NpcController.GetSqrDistanceWithLocalPlayer();
                    float t = Mathf.InverseLerp(1f, UIConst.DISTANCE_SOLID_OUTLINE * UIConst.DISTANCE_SOLID_OUTLINE, distance);
                    rimPower = Mathf.Lerp(UIConst.OUTLINE_RIM_DEFAULT,
                                          UIConst.OUTLINE_RIM_SOLID,
                                          t);
                }

                bool owned = StartOfRound.Instance != null
                            && StartOfRound.Instance.localPlayerController != null
                            && StartOfRound.Instance.localPlayerController.actualClientId == intern.OwnerClientId;

                if (!internStates.TryGetValue(intern.Npc.playerClientId, out OutlineState state))
                {
                    state = new OutlineState();
                    internStates[intern.Npc.playerClientId] = state;
                }

                ApplyState(intern.Npc.gameObject,
                           state,
                           shouldOutline,
                           onlySkinned: true,
                           intensity: UIConst.OUTLINE_INTENSITY_DEFAULT,
                           rimPower,
                           color: owned ? UIConst.UI_COLOR_ORANGE : UIConst.UI_COLOR_DEFAULT);
            }
        }

        public static void UpdateEnemiesOutlines(IEnumerable<EnemyAI> enemies,
                                                  EnemyAI? pointedEnemy,
                                                  bool allowMultiple,
                                                  bool forceNoOutlines = false)
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;

            foreach (EnemyAI enemy in enemies)
            {
                if (enemy == null) continue;

                bool shouldOutline;
                if (forceNoOutlines)
                {
                    shouldOutline = false;
                }
                else
                {
                    shouldOutline = enemy == pointedEnemy || allowMultiple;
                }

                float rimPower = UIConst.OUTLINE_RIM_DEFAULT;
                if (shouldOutline)
                {
                    float sqrDistance = (enemy.transform.position - localPlayer.gameplayCamera.transform.position).sqrMagnitude;
                    float t = Mathf.InverseLerp(1f, UIConst.DISTANCE_SOLID_OUTLINE * UIConst.DISTANCE_SOLID_OUTLINE, sqrDistance);
                    rimPower = Mathf.Lerp(UIConst.OUTLINE_RIM_DEFAULT,
                                          UIConst.OUTLINE_RIM_SOLID,
                                          t);
                }

                if (!enemiesStates.TryGetValue(enemy, out OutlineState state))
                {
                    state = new OutlineState();
                    enemiesStates[enemy] = state;
                }

                ApplyState(enemy.gameObject,
                           state,
                           shouldOutline,
                           onlySkinned: true,
                           intensity: UIConst.OUTLINE_INTENSITY_DEFAULT,
                           rimPower,
                           color: UIConst.UI_COLOR_ORANGE);
            }
        }

        public static void UpdateItemsOutlines(IEnumerable<GameObject> items,
                                               GrabbableObject? pointedItem,
                                               bool allowMultiple,
                                               bool forceNoOutlines = false)
        {
            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
                return;

            foreach (GameObject item in items)
            {
                if (item == null) continue;

                GrabbableObject? grabbableObject = item.GetComponent<GrabbableObject>();
                if (grabbableObject == null)
                {
                    continue;
                }

                bool shouldOutline;
                if (forceNoOutlines)
                {
                    shouldOutline = false;
                }
                else
                {
                    shouldOutline = InternManager.Instance.IsGrabbableObjectGrabbable(grabbableObject, forcePickUp: true)
                                    && (grabbableObject == pointedItem || allowMultiple);
                }

                if (!itemStates.TryGetValue(grabbableObject, out OutlineState state))
                {
                    state = new OutlineState();
                    itemStates[grabbableObject] = state;
                }

                ApplyState(item.gameObject,
                           state,
                           shouldOutline,
                           onlySkinned: false,
                           intensity: UIConst.OUTLINE_INTENSITY_DEFAULT,
                           rimPower: UIConst.OUTLINE_RIM_SOLID,
                           color: UIConst.UI_COLOR_ORANGE); ;
            }
        }

        private static void ApplyState(GameObject target,
                                       OutlineState state,
                                       bool shouldEnable,
                                       bool onlySkinned,
                                       float intensity,
                                       float rimPower,
                                       Color color)
        {
            if (shouldEnable != state.enabled)
            {
                if (shouldEnable)
                {
                    SimpleOutline.Add(target,
                                      color,
                                      onlySkinned,
                                      rimPower,
                                      intensity);
                }
                else
                {
                    SimpleOutline.Remove(target);
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
                    SimpleOutline.UpdateParams(target,
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

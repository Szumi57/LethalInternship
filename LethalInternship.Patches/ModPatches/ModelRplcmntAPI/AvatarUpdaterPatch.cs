using HarmonyLib;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.ManagerProviders;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using ModelReplacement.AvatarBodyUpdater;
using UnityEngine;

namespace LethalInternship.Patches.ModPatches.ModelRplcmntAPI
{
    [HarmonyPatch(typeof(AvatarUpdater))]
    public class AvatarUpdaterPatch
    {
        [HarmonyPatch("UpdateModel")]
        [HarmonyPrefix]
        static bool UpdateModel_Prefix(AvatarUpdater __instance,
                                       GameObject ___player,
                                       SkinnedMeshRenderer ___playerModelRenderer,
                                       Vector3 ___rootPositionOffset)
        {
            // Cull animations ?
            IInternCullingBodyInfo? internCullingBodyInfo = InternManagerProvider.Instance.GetInternCullingBodyInfo(___player.gameObject);
            if (internCullingBodyInfo == null)
            {
                return true;
            }

            UpdateAnimationCullingModelReplacement(internCullingBodyInfo,
                                                   __instance,
                                                   ___rootPositionOffset,
                                                   ___playerModelRenderer);
            return false;
        }

        private static void UpdateAnimationCullingModelReplacement(IInternCullingBodyInfo internCullingBodyInfo,
                                                                   AvatarUpdater avatarBodyUpdater,
                                                                   Vector3 rootPositionOffset,
                                                                   SkinnedMeshRenderer playerModelRenderer)
        {
            internCullingBodyInfo.TimerRagdollUpdateModelReplacement += Time.deltaTime;
            if (internCullingBodyInfo.TimerRagdollUpdateModelReplacement > 10f) internCullingBodyInfo.TimerRagdollUpdateModelReplacement = 10f;

            switch (internCullingBodyInfo.EnumBodyTypeCulling)
            {
                case EnumBodyTypeCulling.InternBody:
                    UpdateAnimationCullingInternBody(internCullingBodyInfo, avatarBodyUpdater, rootPositionOffset, playerModelRenderer);
                    break;
                case EnumBodyTypeCulling.Ragdoll:
                    UpdateAnimationCullingRagdoll(internCullingBodyInfo, avatarBodyUpdater, rootPositionOffset, playerModelRenderer);
                    break;
                default:
                    break;
            }
        }

        private static void UpdateAnimationCullingInternBody(IInternCullingBodyInfo internCullingBodyInfo,
                                                             AvatarUpdater avatarBodyUpdater,
                                                             Vector3 rootPositionOffset,
                                                             SkinnedMeshRenderer playerModelRenderer)
        {
            // Model close in FOV ? full update
            if (internCullingBodyInfo.RankDistanceWithModelReplacementInFOV < PluginRuntimeProvider.Context.Config.MaxModelReplacementModelAnimatedInterns)
            {
                UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);
                UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
                return;
            }

            UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);

            // slow update
            if (internCullingBodyInfo.TimerRagdollUpdateModelReplacement < 0.4f)
            {
                return;
            }
            internCullingBodyInfo.TimerRagdollUpdateModelReplacement = 0f;

            // In fov ?
            if (internCullingBodyInfo.BodyInFOV)
            {
                UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
            }
        }

        private static void UpdateAnimationCullingRagdoll(IInternCullingBodyInfo internCullingBodyInfo,
                                                          AvatarUpdater avatarBodyUpdater,
                                                          Vector3 rootPositionOffset,
                                                          SkinnedMeshRenderer playerModelRenderer)
        {
            // Model close in FOV ? full update
            if (internCullingBodyInfo.RankDistanceWithModelReplacementInFOV < PluginRuntimeProvider.Context.Config.MaxModelReplacementModelAnimatedInterns)
            {
                UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);
                UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
                return;
            }

            // slow update
            if (internCullingBodyInfo.TimerRagdollUpdateModelReplacement < 0.4f)
            {
                return;
            }
            internCullingBodyInfo.TimerRagdollUpdateModelReplacement = 0f;

            // In fov ?
            if (!internCullingBodyInfo.BodyInFOV)
            {
                return;
            }

            UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);
            UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
        }

        private static void UpdateSpineModelReplacement(AvatarUpdater avatarBodyUpdater,
                                                        Vector3 rootPositionOffset)
        {
            Transform avatarTransformFromBoneName = avatarBodyUpdater.GetAvatarTransformFromBoneName("spine");
            Transform playerTransformFromBoneName = avatarBodyUpdater.GetPlayerTransformFromBoneName("spine");
            avatarTransformFromBoneName.position = playerTransformFromBoneName.position + playerTransformFromBoneName.TransformVector(rootPositionOffset);
        }

        private static void UpdateBonesModelReplacement(AvatarUpdater avatarBodyUpdater,
                                                        SkinnedMeshRenderer playerModelRenderer)
        {
            foreach (Transform transform in playerModelRenderer.bones)
            {
                Transform avatarTransformFromBoneName2 = avatarBodyUpdater.GetAvatarTransformFromBoneName(transform.name);
                if (avatarTransformFromBoneName2 != null)
                {
                    avatarTransformFromBoneName2.rotation = transform.rotation;
                    ModelReplacement.AvatarBodyUpdater.RotationOffset component = avatarTransformFromBoneName2.GetComponent<ModelReplacement.AvatarBodyUpdater.RotationOffset>();
                    if (component != null)
                    {
                        avatarTransformFromBoneName2.rotation *= component.offset;
                    }
                }
            }
        }
    }
}

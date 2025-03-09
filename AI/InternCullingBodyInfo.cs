using GameNetcodeStuff;
using LethalInternship.Enums;
using LethalInternship.Managers;
using System.Linq;
using UnityEngine;

namespace LethalInternship.AI
{
    public class InternCullingBodyInfo
    {
        public Component InternBody;
        public EnumBodyTypeCulling EnumBodyTypeCulling;

        public bool BodyInFOV;
        public bool HasModelReplacement;

        public int? RankDistanceAnyModel;
        public int? RankDistanceNoModelReplacement;
        public int? RankDistanceWithModelReplacement;

        public int? RankDistanceAnyModelInFOV;
        public int? RankDistanceNoModelReplacementInFOV;
        public int? RankDistanceWithModelReplacementInFOV;

        private float timerRagdollUpdateModelReplacement;

        public InternCullingBodyInfo(Component internBody, bool hasModelReplacement)
        {
            InternBody = internBody;

            Init(hasModelReplacement);
        }

        public void Init(bool hasModelReplacement)
        {
            BodyInFOV = false;
            HasModelReplacement = hasModelReplacement;
            ResetBodyInfos();

            // For internAI we add directly the culling info
            PlayerControllerB? playerController = InternBody as PlayerControllerB;
            if (playerController != null)
            {
                this.EnumBodyTypeCulling = EnumBodyTypeCulling.InternBody;

                InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI != null)
                {
                    internAI.NpcController.InternCullingBodyInfo = this;
                }
            }

            DeadBodyInfo? deadBodyInfo = InternBody as DeadBodyInfo;
            if (deadBodyInfo != null)
            {
                this.EnumBodyTypeCulling = EnumBodyTypeCulling.Ragdoll;
            }
        }

        public void ResetBodyInfos()
        {
            BodyInFOV = false;

            RankDistanceAnyModel = null;
            RankDistanceWithModelReplacement = null;
            RankDistanceNoModelReplacement = null;

            RankDistanceAnyModelInFOV = null;
            RankDistanceNoModelReplacementInFOV = null;
            RankDistanceWithModelReplacementInFOV = null;
        }

        public bool IsRankDistanceAnyModelInFOVValid(int rankDistanceMax)
        {
            return RankDistanceAnyModelInFOV.HasValue
                    && RankDistanceAnyModelInFOV.Value < rankDistanceMax;
        }

        public float GetSqrDistanceWithLocalPlayer()
        {
            if (InternBody == null)
            {
                return float.MaxValue;
            }

            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
            {
                return float.MaxValue;
            }

            PlayerControllerB? playerController = InternBody as PlayerControllerB;
            if (playerController != null)
            {
                InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI != null)
                {
                    if (internAI.isEnemyDead
                        || internAI.NpcController == null
                        || !internAI.NpcController.Npc.isPlayerControlled
                        || internAI.NpcController.Npc.isPlayerDead)
                    {
                        return float.MaxValue;
                    }

                    return internAI.NpcController.SqrDistanceWithLocalPlayerTimedCheck.GetSqrDistanceWithLocalPlayer(internAI.NpcController.Npc.transform.position);
                }
            }

            DeadBodyInfo? deadBodyInfo = InternBody as DeadBodyInfo;
            if (deadBodyInfo != null)
            {
                return (StartOfRound.Instance.localPlayerController.transform.position - deadBodyInfo.transform.position).sqrMagnitude;
            }

            return float.MaxValue;
        }

        public bool CheckIsInFOV()
        {
            if (InternBody == null)
            {
                return false;
            }

            if (StartOfRound.Instance == null
                || StartOfRound.Instance.localPlayerController == null)
            {
                return false;
            }

            Camera localPlayerCamera = StartOfRound.Instance.localPlayerController.gameplayCamera;

            PlayerControllerB? playerController = InternBody as PlayerControllerB;
            if (playerController != null)
            {
                InternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI != null)
                {
                    if (internAI.isEnemyDead
                        || internAI.NpcController == null
                        || !internAI.NpcController.Npc.isPlayerControlled
                        || internAI.NpcController.Npc.isPlayerDead)
                    {
                        return false;
                    }

                    Vector3 internBodyPos = internAI.NpcController.Npc.transform.position + new Vector3(0, 1.7f, 0);
                    return internAI.AngleFOVWithLocalPlayerTimedCheck.GetAngleFOVWithLocalPlayer(localPlayerCamera.transform, internBodyPos) < localPlayerCamera.fieldOfView * 0.81f;
                }
            }

            DeadBodyInfo? deadBodyInfo = InternBody as DeadBodyInfo;
            if (deadBodyInfo != null)
            {
                if (InternManager.Instance.HeldInternsLocalPlayer.Contains(deadBodyInfo.playerObjectId))
                {
                    // Held by local player
                    if (InternManager.Instance.HeldInternsLocalPlayer.First() == deadBodyInfo.playerObjectId)
                    {
                        // First held force fov
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return Vector3.Angle(localPlayerCamera.transform.forward, (deadBodyInfo.transform.position - localPlayerCamera.transform.position)) < localPlayerCamera.fieldOfView * 0.81f;
                }
            }

            return false;
        }

        public void UpdateAnimationCullingModelReplacement(ModelReplacement.AvatarBodyUpdater.AvatarUpdater avatarBodyUpdater,
                                                           Vector3 rootPositionOffset,
                                                           SkinnedMeshRenderer playerModelRenderer)
        {
            timerRagdollUpdateModelReplacement += Time.deltaTime;
            if (timerRagdollUpdateModelReplacement > 10f) timerRagdollUpdateModelReplacement = 10f;

            switch (this.EnumBodyTypeCulling)
            {
                case EnumBodyTypeCulling.InternBody:
                    UpdateAnimationCullingInternBody(avatarBodyUpdater, rootPositionOffset, playerModelRenderer);
                    break;
                case EnumBodyTypeCulling.Ragdoll:
                    UpdateAnimationCullingRagdoll(avatarBodyUpdater, rootPositionOffset, playerModelRenderer);
                    break;
                default:
                    break;
            }
        }

        private void UpdateAnimationCullingInternBody(ModelReplacement.AvatarBodyUpdater.AvatarUpdater avatarBodyUpdater,
                                                      Vector3 rootPositionOffset,
                                                      SkinnedMeshRenderer playerModelRenderer)
        {
            // Model close in FOV ? full update
            if (RankDistanceWithModelReplacementInFOV < Plugin.Config.MaxModelReplacementModelAnimatedInterns.Value)
            {
                UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);
                UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
                return;
            }

            UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);

            // slow update
            if (timerRagdollUpdateModelReplacement < 0.4f)
            {
                return;
            }
            timerRagdollUpdateModelReplacement = 0f;

            // In fov ?
            if (BodyInFOV)
            {
                UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
            }
        }

        private void UpdateAnimationCullingRagdoll(ModelReplacement.AvatarBodyUpdater.AvatarUpdater avatarBodyUpdater,
                                                   Vector3 rootPositionOffset,
                                                   SkinnedMeshRenderer playerModelRenderer)
        {
            // Model close in FOV ? full update
            if (RankDistanceWithModelReplacementInFOV < Plugin.Config.MaxModelReplacementModelAnimatedInterns.Value)
            {
                UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);
                UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
                return;
            }

            // slow update
            if (timerRagdollUpdateModelReplacement < 0.4f)
            {
                return;
            }
            timerRagdollUpdateModelReplacement = 0f;

            // In fov ?
            if (!BodyInFOV)
            {
                return;
            }

            UpdateSpineModelReplacement(avatarBodyUpdater, rootPositionOffset);
            UpdateBonesModelReplacement(avatarBodyUpdater, playerModelRenderer);
        }

        private void UpdateSpineModelReplacement(ModelReplacement.AvatarBodyUpdater.AvatarUpdater avatarBodyUpdater,
                                                 Vector3 rootPositionOffset)
        {
            Transform avatarTransformFromBoneName = avatarBodyUpdater.GetAvatarTransformFromBoneName("spine");
            Transform playerTransformFromBoneName = avatarBodyUpdater.GetPlayerTransformFromBoneName("spine");
            avatarTransformFromBoneName.position = playerTransformFromBoneName.position + playerTransformFromBoneName.TransformVector(rootPositionOffset);
        }

        private void UpdateBonesModelReplacement(ModelReplacement.AvatarBodyUpdater.AvatarUpdater avatarBodyUpdater,
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

using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.Interns;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.Interns
{
    public class InternCullingBodyInfo : IInternCullingBodyInfo
    {
        public Component InternBody { get => internBody; set => internBody = value; }
        public EnumBodyTypeCulling EnumBodyTypeCulling { get => enumBodyTypeCulling; set => enumBodyTypeCulling = value; }
        public bool BodyInFOV { get => bodyInFOV; set => bodyInFOV = value; }
        public bool HasModelReplacement { get => hasModelReplacement; set => hasModelReplacement = value; }
        public int? RankDistanceAnyModel { get => rankDistanceAnyModel; set => rankDistanceAnyModel = value; }
        public int? RankDistanceNoModelReplacement { get => rankDistanceNoModelReplacement; set => rankDistanceNoModelReplacement = value; }
        public int? RankDistanceWithModelReplacement { get => rankDistanceWithModelReplacement; set => rankDistanceWithModelReplacement = value; }
        public int? RankDistanceAnyModelInFOV { get => rankDistanceAnyModelInFOV; set => rankDistanceAnyModelInFOV = value; }
        public int? RankDistanceNoModelReplacementInFOV { get => rankDistanceNoModelReplacementInFOV; set => rankDistanceNoModelReplacementInFOV = value; }
        public int? RankDistanceWithModelReplacementInFOV { get => rankDistanceWithModelReplacementInFOV; set => rankDistanceWithModelReplacementInFOV = value; }
        public float TimerRagdollUpdateModelReplacement { get => timerRagdollUpdateModelReplacement; set => timerRagdollUpdateModelReplacement = value; }
        
        private Component internBody;
        private EnumBodyTypeCulling enumBodyTypeCulling;

        private bool bodyInFOV;
        private bool hasModelReplacement;

        private int? rankDistanceAnyModel;
        private int? rankDistanceNoModelReplacement;
        private int? rankDistanceWithModelReplacement;
        private int? rankDistanceAnyModelInFOV;

        private int? rankDistanceNoModelReplacementInFOV;
        private int? rankDistanceWithModelReplacementInFOV;

        private float timerRagdollUpdateModelReplacement;

        public InternCullingBodyInfo(Component internBody, bool hasModelReplacement)
        {
            this.internBody = internBody;

            Init(hasModelReplacement);
        }

        public void Init(bool hasModelReplacement)
        {
            this.bodyInFOV = false;
            this.hasModelReplacement = hasModelReplacement;
            ResetBodyInfos();

            // For internAI we add directly the culling info
            PlayerControllerB? playerController = InternBody as PlayerControllerB;
            if (playerController != null)
            {
                EnumBodyTypeCulling = EnumBodyTypeCulling.InternBody;

                IInternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI != null)
                {
                    internAI.NpcController.InternCullingBodyInfo = this;
                }
            }

            DeadBodyInfo? deadBodyInfo = InternBody as DeadBodyInfo;
            if (deadBodyInfo != null)
            {
                EnumBodyTypeCulling = EnumBodyTypeCulling.Ragdoll;
            }
        }

        public void ResetBodyInfos()
        {
            this.bodyInFOV = false;
            
            this.rankDistanceAnyModel = null;
            this.rankDistanceWithModelReplacement = null;
            this.rankDistanceNoModelReplacement = null;
            
            this.rankDistanceAnyModelInFOV = null;
            this.rankDistanceNoModelReplacementInFOV = null;
            this.rankDistanceWithModelReplacementInFOV = null;
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
                IInternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI != null)
                {
                    if (internAI.IsEnemyDead
                        || internAI.NpcController == null
                        || !internAI.NpcController.Npc.isPlayerControlled
                        || internAI.NpcController.Npc.isPlayerDead)
                    {
                        return float.MaxValue;
                    }

                    return internAI.NpcController.GetSqrDistanceWithLocalPlayer(internAI.NpcController.Npc.transform.position);
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
                IInternAI? internAI = InternManager.Instance.GetInternAI((int)playerController.playerClientId);
                if (internAI != null)
                {
                    if (internAI.IsEnemyDead
                        || internAI.NpcController == null
                        || !internAI.NpcController.Npc.isPlayerControlled
                        || internAI.NpcController.Npc.isPlayerDead)
                    {
                        return false;
                    }

                    Vector3 internBodyPos = internAI.NpcController.Npc.transform.position + new Vector3(0, 1.7f, 0);
                    return internAI.GetAngleFOVWithLocalPlayer(localPlayerCamera.transform, internBodyPos) < localPlayerCamera.fieldOfView * 0.81f;
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
                    return Vector3.Angle(localPlayerCamera.transform.forward, deadBodyInfo.transform.position - localPlayerCamera.transform.position) < localPlayerCamera.fieldOfView * 0.81f;
                }
            }

            return false;
        }
    }
}

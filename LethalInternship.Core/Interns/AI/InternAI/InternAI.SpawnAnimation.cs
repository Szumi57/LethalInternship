using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.ModelReplacementAPIHooks;
using LethalInternship.SharedAbstractions.Parameters;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        private Coroutine? spawnAnimationCoroutine = null!;
        private bool animationCoroutineRagdollingRunning = false;

        #region Spawn animation

        public bool IsSpawningAnimationRunning()
        {
            return spawnAnimationCoroutine != null;
        }

        public Coroutine BeginInternSpawnAnimation(EnumSpawnAnimation enumSpawnAnimation)
        {
            switch (enumSpawnAnimation)
            {
                case EnumSpawnAnimation.None:
                    return StartCoroutine(CoroutineNoSpawnAnimation());

                case EnumSpawnAnimation.OnlyPlayerSpawnAnimation:
                    return StartCoroutine(CoroutineOnlyPlayerSpawnAnimation());

                case EnumSpawnAnimation.RagdollFromDropShipAndPlayerSpawnAnimation:
                    return StartCoroutine(CoroutineFromDropShipAndPlayerSpawnAnimation());

                default:
                    return StartCoroutine(CoroutineNoSpawnAnimation());
            }
        }

        private IEnumerator CoroutineNoSpawnAnimation()
        {
            if (!IsOwner)
            {
                spawnAnimationCoroutine = null;
                yield break;
            }

            if (IsOwner)
            {
                // Change ai state
                SyncAssignTargetAndSetMovingTo(GetClosestIrlPlayer());
            }

            yield return null;

            if (IsOwner)
            {
                // Teleport again, cuz I don't know why the teleport does not work first time
                TeleportAgentAIAndBody(GameNetworkManager.Instance.localPlayerController.transform.position);
            }

            spawnAnimationCoroutine = null;
            yield break;
        }

        private IEnumerator CoroutineOnlyPlayerSpawnAnimation()
        {
            if (!IsOwner)
            {
                // Wait for spawn player animation
                yield return new WaitForSeconds(3f);
                NpcController.Npc.inSpecialInteractAnimation = false;
                spawnAnimationCoroutine = null;
                yield break;
            }

            UpdateInternSpecialAnimationValue(specialAnimation: true, timed: 0f, climbingLadder: false);
            NpcController.Npc.inSpecialInteractAnimation = true;
            NpcController.Npc.playerBodyAnimator.ResetTrigger("SpawnPlayer");
            NpcController.Npc.playerBodyAnimator.SetTrigger("SpawnPlayer");

            yield return new WaitForSeconds(3f);

            NpcController.Npc.inSpecialInteractAnimation = false;
            UpdateInternSpecialAnimationValue(specialAnimation: false, timed: 0f, climbingLadder: false);

            // Change ai state
            SyncAssignTargetAndSetMovingTo(GetClosestIrlPlayer());

            spawnAnimationCoroutine = null;
            yield break;
        }

        private IEnumerator CoroutineFromDropShipAndPlayerSpawnAnimation()
        {
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                // Wait for model replacement to add its component
                yield return new WaitForEndOfFrame();
                // Wait for  model replacement to init replacement models
                yield return new WaitForEndOfFrame();
            }

            animationCoroutineRagdollingRunning = true;
            PlayerControllerB closestPlayer = GetClosestIrlPlayer();

            // Spawn ragdoll
            InstantiateDeadBodyInfo(closestPlayer, GetRandomPushForce(InternManager.Instance.ItemDropShipPos + new Vector3(0, -1f, 0), NpcController.Npc.transform.position, 4f));
            RagdollInternBody.SetFreeRagdoll(ragdollBodyDeadBodyInfo);

            // Hide intern
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowReplacementModelOnlyBody?.Invoke(Npc, this, show: false);
            }
            else
            {
                InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: false, disableLocalArms: false);
                HideShowLevelStickerBetaBadge(show: false);
            }

            // Hide items
            HeldItems.ShowHideAllItemsMeshes(show: false, includeHeldWeapon: true);

            yield return null;

            // Voice
            InternIdentity.Voice.TryPlayVoiceAudio(new PlayVoiceParameters()
            {
                VoiceState = EnumVoicesState.Hit,
                CanTalkIfOtherInternTalk = true,
                WaitForCooldown = false,
                CutCurrentVoiceStateToTalk = true,
                CanRepeatVoiceState = false,

                ShouldSync = false,
                IsInternInside = NpcController.Npc.isInsideFactory,
                AllowSwearing = PluginRuntimeProvider.Context.Config.AllowSwearing
            });

            // Wait in ragdoll state
            yield return new WaitForSeconds(2.5f);
            // End of ragdoll wait

            animationCoroutineRagdollingRunning = false;

            // Enable model
            if (PluginRuntimeProvider.Context.IsModModelReplacementAPILoaded)
            {
                ModelReplacementAPIHook.HideShowReplacementModelOnlyBody?.Invoke(Npc, this, show: true);
            }
            else
            {
                InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: true, disableLocalArms: true);
                HideShowLevelStickerBetaBadge(show: true);
            }

            // Show items
            HeldItems.ShowHideAllItemsMeshes(show: true, includeHeldWeapon: true);

            // Hide ragdoll
            RagdollInternBody.Hide();

            if (!IsOwner)
            {
                // Wait for spawn player animation
                yield return new WaitForSeconds(3f);
                NpcController.Npc.inSpecialInteractAnimation = false;
                spawnAnimationCoroutine = null;
                yield break;
            }

            DeadBodyInfo? deadBodyInfo = RagdollInternBody.GetDeadBodyInfo();
            TeleportAgentAIAndBody(deadBodyInfo == null ? NpcController.Npc.transform.position : deadBodyInfo.transform.position);
            UpdateInternSpecialAnimationValue(specialAnimation: true, timed: 0f, climbingLadder: false);
            NpcController.Npc.inSpecialInteractAnimation = true;
            NpcController.Npc.playerBodyAnimator.ResetTrigger("SpawnPlayer");
            NpcController.Npc.playerBodyAnimator.SetTrigger("SpawnPlayer");

            // Wait in spawn player animation
            yield return new WaitForSeconds(3f);

            NpcController.Npc.inSpecialInteractAnimation = false;
            UpdateInternSpecialAnimationValue(specialAnimation: false, timed: 0f, climbingLadder: false);

            spawnAnimationCoroutine = null;

            // Change ai state
            SyncAssignTargetAndSetMovingTo(closestPlayer);
            yield break;
        }

        private PlayerControllerB GetClosestIrlPlayer()
        {
            PlayerControllerB closest = null!;
            for (int i = 0; i < InternManager.Instance.IndexBeginOfInterns; i++)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                if (!player.isPlayerControlled
                    || player.isPlayerDead)
                {
                    continue;
                }

                if (closest == null
                   || (player.transform.position - NpcController.Npc.transform.position).sqrMagnitude < (closest.transform.position - NpcController.Npc.transform.position).sqrMagnitude)
                {
                    closest = player;
                }
            }

            return closest;
        }

        private Vector3 GetRandomPushForce(Vector3 origin, Vector3 point, float forceMean)
        {
            point.y += UnityEngine.Random.Range(2f, 4f);

            //DrawUtil.DrawWhiteLine(LineRendererUtil.GetLineRenderer(), new Ray(origin, point - origin), Vector3.Distance(point, origin));
            float force = UnityEngine.Random.Range(forceMean * 0.5f, forceMean * 1.5f);
            return Vector3.Normalize(point - origin) * force / Vector3.Distance(point, origin);
        }

        #endregion
    }
}

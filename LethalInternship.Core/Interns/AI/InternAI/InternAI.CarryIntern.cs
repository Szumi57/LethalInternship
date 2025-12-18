using GameNetcodeStuff;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        private DeadBodyInfo ragdollBodyDeadBodyInfo = null!;

        #region Grab intern

        [ServerRpc(RequireOwnership = false)]
        public void GrabInternServerRpc(ulong idPlayerGrabberController)
        {
            GrabInternClientRpc(idPlayerGrabberController);
        }

        [ClientRpc]
        private void GrabInternClientRpc(ulong idPlayerGrabberController)
        {
            PlayerControllerB playerGrabberController = StartOfRound.Instance.allPlayerScripts[idPlayerGrabberController];

            InstantiateDeadBodyInfo(playerGrabberController);
            RagdollInternBody.SetGrabbedBy(playerGrabberController,
                                           ragdollBodyDeadBodyInfo,
                                           (int)idPlayerGrabberController);

            if (idPlayerGrabberController == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                // Add weight of body
                float weightToGain = RagdollInternBody.GetWeight() - 1f < 0f ? 0f : RagdollInternBody.GetWeight() - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight + weightToGain, 1f, 10f);

                weightToGain = NpcController.Npc.carryWeight - 1f < 0f ? 0f : NpcController.Npc.carryWeight - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight + weightToGain, 1f, 10f);

                // Register held interns
                InternManager.Instance.RegisterHeldInternForLocalPlayer((int)NpcController.Npc.playerClientId);
                // Hide of held ragdoll > 1 is done on BodyReplacementBasePatch after creation of replacementDeadBody
            }

            HeldItems.ShowHideAllItemsMeshes(show: false);

            // Hide intern
            NpcController.Npc.localVisor.position = NpcController.Npc.playersManager.notSpawnedPosition.position;
            InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: false, disableLocalArms: false);
            NpcController.Npc.transform.position = NpcController.Npc.playersManager.notSpawnedPosition.position;

            StopSinkingState();
            NpcController.Npc.ResetFallGravity();
            NpcController.OrderToStopMoving();

            // Register body for animation culling
            InternManager.Instance.RegisterInternBodyForAnimationCulling(ragdollBodyDeadBodyInfo, HasInternModelReplacementAPI());
        }

        private void InstantiateDeadBodyInfo(PlayerControllerB playerReference, Vector3 bodyVelocity = default)
        {
            float num = 1.32f;
            int deathAnimation = 0;

            Transform parent = null!;
            if (playerReference.isInElevator)
            {
                parent = playerReference.playersManager.elevatorTransform;
            }

            Vector3 position = NpcController.Npc.thisPlayerBody.position + Vector3.up * num;
            Quaternion rotation = NpcController.Npc.thisPlayerBody.rotation;
            if (ragdollBodyDeadBodyInfo == null)
            {
                GameObject gameObject = Instantiate(NpcController.Npc.playersManager.playerRagdolls[deathAnimation],
                                                                       position,
                                                                       rotation,
                                                                       parent);
                ragdollBodyDeadBodyInfo = gameObject.GetComponent<DeadBodyInfo>();
            }

            ragdollBodyDeadBodyInfo.transform.position = position;
            ragdollBodyDeadBodyInfo.transform.rotation = rotation;
            ragdollBodyDeadBodyInfo.transform.parent = parent;

            if (playerReference.physicsParent != null)
            {
                ragdollBodyDeadBodyInfo.SetPhysicsParent(playerReference.physicsParent);
            }

            ragdollBodyDeadBodyInfo.parentedToShip = playerReference.isInElevator;
            ragdollBodyDeadBodyInfo.playerObjectId = (int)NpcController.Npc.playerClientId;

            Rigidbody[] componentsInChildren = ragdollBodyDeadBodyInfo.gameObject.GetComponentsInChildren<Rigidbody>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].velocity = bodyVelocity;
            }

            // Scale ragdoll (without stretching the body parts)
            ResizeRagdoll(ragdollBodyDeadBodyInfo.transform);

            // False with model replacement API
            ragdollBodyDeadBodyInfo.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;

            // Set suit ID
            if (ragdollBodyDeadBodyInfo.setMaterialToPlayerSuit)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = ragdollBodyDeadBodyInfo.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderer.sharedMaterial = StartOfRound.Instance.unlockablesList.unlockables[NpcController.Npc.currentSuitID].suitMaterial;
                    skinnedMeshRenderer.renderingLayerMask = 513U | 1U << ragdollBodyDeadBodyInfo.playerObjectId + 12;
                }
            }
        }

        /// <summary>
        /// Scale ragdoll (without stretching the body parts)
        /// </summary>
        /// <param name="transform"></param>
        private void ResizeRagdoll(Transform transform)
        {
            // https://discussions.unity.com/t/joint-system-scale-problems/182154/4
            // https://stackoverflow.com/questions/68663372/how-to-enlarge-a-ragdoll-in-game-unity
            // Grab references to joints anchors, to update them during the game.
            Joint[] joints;
            List<Vector3> connectedAnchors = new List<Vector3>();
            List<Vector3> anchors = new List<Vector3>();
            joints = transform.GetComponentsInChildren<Joint>();

            Joint curJoint;
            for (int i = 0; i < joints.Length; i++)
            {
                curJoint = joints[i];
                connectedAnchors.Add(curJoint.connectedAnchor);
                anchors.Add(curJoint.anchor);
            }

            transform.localScale = new Vector3(PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale, PluginRuntimeProvider.Context.Config.InternSizeScale);

            // Update joints by resetting them to their original values
            Joint joint;
            for (int i = 0; i < joints.Length; i++)
            {
                joint = joints[i];
                joint.connectedAnchor = connectedAnchors[i];
                joint.anchor = anchors[i];
            }
        }

        #endregion

        #region Release intern

        public void SyncReleaseIntern(PlayerControllerB playerGrabberController)
        {
            // Make the pos slightly different so the interns separate on teleport
            Random randomInstance = new Random();
            Vector3 randomPos = new Vector3(playerGrabberController.transform.position.x + (float)randomInstance.NextDouble() * 0.1f,
                                            playerGrabberController.transform.position.y,
                                            playerGrabberController.transform.position.z + (float)randomInstance.NextDouble() * 0.1f);

            if (IsServer)
            {
                ReleaseInternClientRpc(playerGrabberController.playerClientId,
                                       randomPos,
                                       !playerGrabberController.isInsideFactory,
                                       isUsingEntrance: false);
            }
            else
            {
                ReleaseInternServerRpc(playerGrabberController.playerClientId,
                                       randomPos,
                                       !playerGrabberController.isInsideFactory,
                                       isUsingEntrance: false);
            }
        }

        [ServerRpc]
        private void ReleaseInternServerRpc(ulong idPlayerGrabberController,
                                            Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            ReleaseInternClientRpc(idPlayerGrabberController, pos, setOutside, isUsingEntrance);
        }

        [ClientRpc]
        private void ReleaseInternClientRpc(ulong idPlayerGrabberController,
                                            Vector3 pos, bool setOutside, bool isUsingEntrance)
        {
            ReleaseIntern(idPlayerGrabberController);
            TeleportIntern(pos, setOutside, isUsingEntrance);
        }

        private void ReleaseIntern(ulong idPlayerGrabberController)
        {
            if (idPlayerGrabberController == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                // Remove weight of body
                PlayerControllerB playerGrabberController = StartOfRound.Instance.allPlayerScripts[idPlayerGrabberController];
                float weightToLose = RagdollInternBody.GetWeight() - 1f < 0f ? 0f : RagdollInternBody.GetWeight() - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight - weightToLose, 1f, 10f);

                weightToLose = NpcController.Npc.carryWeight - 1f < 0f ? 0f : NpcController.Npc.carryWeight - 1f;
                playerGrabberController.carryWeight = Mathf.Clamp(playerGrabberController.carryWeight - weightToLose, 1f, 10f);

                // Unregister held interns
                InternManager.Instance.UnregisterHeldInternForLocalPlayer((int)NpcController.Npc.playerClientId);
                InternManager.Instance.HideShowRagdollModel(NpcController.Npc, show: true);
            }

            HeldItems.ShowHideAllItemsMeshes(show: true);

            RagdollInternBody.Hide();

            // Enable model
            InternManager.Instance.DisableInternControllerModel(NpcController.Npc.gameObject, NpcController.Npc, enable: true, disableLocalArms: true);

            // Set intern to follow
            SetCommandToFollowPlayer();
        }

        #endregion
    }
}

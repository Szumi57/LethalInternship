using GameNetcodeStuff;
using UnityEngine;

namespace LethalInternship.AI
{
    internal class RagdollInternBody
    {
        private int idPlayerHolder = -1;
        private RagdollGrabbableObject ragdollGrabbableObject;

        public RagdollInternBody(RagdollGrabbableObject ragdollGrabbableObject)
        {
            this.ragdollGrabbableObject = ragdollGrabbableObject;
            this.ragdollGrabbableObject.transform.position = StartOfRound.Instance.notSpawnedPosition.position;
            this.idPlayerHolder = -1;
        }

        public void SetGrabbedBy(PlayerControllerB playerGrabberController,
                                 DeadBodyInfo deadBodyInfo,
                                 int idPlayerHolder)
        {
            int bodyPart = 1;

            ragdollGrabbableObject.ragdoll = deadBodyInfo;
            ragdollGrabbableObject.ragdoll.grabBodyObject = ragdollGrabbableObject;
            ragdollGrabbableObject.ragdoll.attachedTo = this.ragdollGrabbableObject.transform;
            ragdollGrabbableObject.ragdoll.attachedLimb = this.ragdollGrabbableObject.ragdoll.bodyParts[bodyPart];
            ragdollGrabbableObject.ragdoll.matchPositionExactly = true;
            ragdollGrabbableObject.ragdoll.lerpBeforeMatchingPosition = true;
            ragdollGrabbableObject.ragdoll.SetBodyPartsKinematic(false);
            ragdollGrabbableObject.ragdoll.gameObject.SetActive(true);
            ragdollGrabbableObject.ragdoll.deactivated = false;

            Transform? transformParent = playerGrabberController.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/shoulder.L/arm.L_upper/arm.L_lower/hand.L");
            if (transformParent == null)
            {
                transformParent = playerGrabberController.localItemHolder;
            }
            ragdollGrabbableObject.parentObject = transformParent;
            ragdollGrabbableObject.transform.SetParent(transformParent);

            //TreesUtils.PrintTransformTree(playerGrabberController.gameObject.GetComponentsInChildren<Transform>());

            ragdollGrabbableObject.hasHitGround = false;
            ragdollGrabbableObject.isInFactory = playerGrabberController.isInsideFactory;
            ragdollGrabbableObject.isHeld = true;
            this.idPlayerHolder = idPlayerHolder;

            //for (int i = 0; i < this.ragdollGrabbableObject.ragdoll.bodyParts.Length; i++)
            //{
            //    Plugin.LogDebug($"{i} {this.ragdollGrabbableObject.ragdoll.bodyParts[i].name}");
            //}
        }

        public void Hide()
        {
            ragdollGrabbableObject.ragdoll.attachedTo = null;
            ragdollGrabbableObject.ragdoll.attachedLimb = null;
            ragdollGrabbableObject.ragdoll.matchPositionExactly = false;
            ragdollGrabbableObject.ragdoll.lerpBeforeMatchingPosition = false;
            ragdollGrabbableObject.ragdoll.gameObject.SetActive(false);
            ragdollGrabbableObject.ragdoll.deactivated = true;

            ragdollGrabbableObject.parentObject = null;
            ragdollGrabbableObject.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);

            ragdollGrabbableObject.hasHitGround = false;
            ragdollGrabbableObject.isHeld = false;
            this.idPlayerHolder = -1;
        }

        public void SetFreeRagdoll(DeadBodyInfo deadBodyInfo)
        {
            ragdollGrabbableObject.ragdoll = deadBodyInfo;
            ragdollGrabbableObject.ragdoll.grabBodyObject = ragdollGrabbableObject;
            ragdollGrabbableObject.parentObject = ragdollGrabbableObject.ragdoll.bodyParts[5].transform;
            ragdollGrabbableObject.transform.SetParent(ragdollGrabbableObject.ragdoll.bodyParts[5].transform);
        }

        public float GetWeight()
        {
            return ragdollGrabbableObject.itemProperties.weight;
        }

        public DeadBodyInfo? GetDeadBodyInfo()
        {
            return ragdollGrabbableObject?.ragdoll;
        }
        public bool IsRagdollBodyHeld()
        {
            return ragdollGrabbableObject.isHeld;
        }

        public bool IsRagdollBodyHeldByPlayer(int idPlayer)
        {
            return idPlayer == idPlayerHolder && IsRagdollBodyHeld();
        }

        public PlayerControllerB GetPlayerHolder()
        {
            return StartOfRound.Instance.allPlayerScripts[idPlayerHolder];
        }

        public bool IsRagdollEnabled()
        {
            return ragdollGrabbableObject.ragdoll != null && !ragdollGrabbableObject.ragdoll.deactivated;
        }
    }
}

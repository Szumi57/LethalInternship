using GameNetcodeStuff;
using LethalInternship.Patches.ObjectsPatches;
using LethalInternship.Utils;
using UnityEngine;

namespace LethalInternship.AI
{
    internal class RagdollInternBody
    {
        public int IdPlayerHolder;

        private RagdollGrabbableObject ragdollGrabbableObject;

        public RagdollInternBody(RagdollGrabbableObject ragdollGrabbableObject, 
                                 DeadBodyInfo deadBodyInfo,
                                 int idPlayerHolder)
        {
            this.ragdollGrabbableObject = ragdollGrabbableObject;
            this.ragdollGrabbableObject.ragdoll = deadBodyInfo;
            this.IdPlayerHolder = idPlayerHolder;

            //for (int i = 0; i < this.ragdollGrabbableObject.ragdoll.bodyParts.Length; i++)
            //{
            //    Plugin.LogDebug($"{i} {this.ragdollGrabbableObject.ragdoll.bodyParts[i].name}");
            //}
        }

        public void Update()
        {
            GrabbableObjectPatch.GrabbableObject_Update_ReversePatch(ragdollGrabbableObject);
            ragdollGrabbableObject.grabbableToEnemies = false;
        }

        public void SetGrabbedBy(PlayerControllerB playerGrabberController)
        {
            int bodyPart = 1;
            ragdollGrabbableObject.gameObject.SetActive(true);
            ragdollGrabbableObject.ragdoll.gameObject.SetActive(true);

            ragdollGrabbableObject.ragdoll.attachedTo = this.ragdollGrabbableObject.transform;
            ragdollGrabbableObject.ragdoll.attachedLimb = this.ragdollGrabbableObject.ragdoll.bodyParts[bodyPart];
            ragdollGrabbableObject.transform.SetParent(this.ragdollGrabbableObject.ragdoll.bodyParts[bodyPart].transform);
            ragdollGrabbableObject.ragdoll.matchPositionExactly = true;
            ragdollGrabbableObject.ragdoll.lerpBeforeMatchingPosition = true;
            ragdollGrabbableObject.ragdoll.SetBodyPartsKinematic(false);

            ragdollGrabbableObject.isHeld = true;
            //TreesUtils.PrintTransformTree(playerGrabberController.gameObject.GetComponentsInChildren<Transform>());
            //ragdollGrabbableObject.parentObject = playerGrabberController.localItemHolder;
            ragdollGrabbableObject.parentObject = playerGrabberController.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/shoulder.L/arm.L_upper/arm.L_lower/hand.L");
            ragdollGrabbableObject.hasHitGround = false;
            ragdollGrabbableObject.isInFactory = playerGrabberController.isInsideFactory;
        }

        public void SetReleased()
        {
            ragdollGrabbableObject.ragdoll.attachedTo = null;
            ragdollGrabbableObject.ragdoll.attachedLimb = null;
            ragdollGrabbableObject.ragdoll.matchPositionExactly = false;
            ragdollGrabbableObject.ragdoll.lerpBeforeMatchingPosition = false;

            ragdollGrabbableObject.isHeld = false;
            ragdollGrabbableObject.parentObject = null;
            ragdollGrabbableObject.hasHitGround = false;
            ragdollGrabbableObject.ragdoll.gameObject.SetActive(false);
            ragdollGrabbableObject.gameObject.SetActive(false);
        }

        public float GetWeight()
        {
            return ragdollGrabbableObject.itemProperties.weight;
        }

        public RagdollGrabbableObject GetRagdollGrabbableObject()
        {
            return ragdollGrabbableObject;
        }

        public DeadBodyInfo? GetDeadBodyInfo()
        {
            return ragdollGrabbableObject?.ragdoll;
        }

        public bool IsRagdollBodyHeldByPlayer(int idPlayer)
        {
            return idPlayer == IdPlayerHolder && ragdollGrabbableObject.isHeld;
        }
    }
}

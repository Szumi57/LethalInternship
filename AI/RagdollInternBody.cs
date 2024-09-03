using GameNetcodeStuff;
using LethalInternship.Patches.ObjectsPatches;
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

        public static void Update_Patch(RagdollGrabbableObject ragdollGrabbableObject)
        {
            GrabbableObjectPatch.GrabbableObject_Update_ReversePatch(ragdollGrabbableObject);
            ragdollGrabbableObject.grabbableToEnemies = false;
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
            ragdollGrabbableObject.transform.SetParent(this.ragdollGrabbableObject.ragdoll.bodyParts[bodyPart].transform);
            ragdollGrabbableObject.ragdoll.matchPositionExactly = true;
            ragdollGrabbableObject.ragdoll.lerpBeforeMatchingPosition = true;
            ragdollGrabbableObject.ragdoll.SetBodyPartsKinematic(false);
            ragdollGrabbableObject.ragdoll.gameObject.SetActive(true);
            ragdollGrabbableObject.ragdoll.deactivated = false;
            for (int i = 0; i < ragdollGrabbableObject.ragdoll.bodyParts.Length; i++)
            {
                ragdollGrabbableObject.ragdoll.bodyParts[i].GetComponent<Collider>().enabled = false;
            }

            ragdollGrabbableObject.isHeld = true;
            //TreesUtils.PrintTransformTree(playerGrabberController.gameObject.GetComponentsInChildren<Transform>());
            //ragdollGrabbableObject.parentObject = playerGrabberController.localItemHolder;
            ragdollGrabbableObject.parentObject = playerGrabberController.gameObject.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/shoulder.L/arm.L_upper/arm.L_lower/hand.L");
            ragdollGrabbableObject.hasHitGround = false;
            ragdollGrabbableObject.isInFactory = playerGrabberController.isInsideFactory;

            this.idPlayerHolder = idPlayerHolder;

            //for (int i = 0; i < this.ragdollGrabbableObject.ragdoll.bodyParts.Length; i++)
            //{
            //    Plugin.LogDebug($"{i} {this.ragdollGrabbableObject.ragdoll.bodyParts[i].name}");
            //}
        }

        public void SetReleased()
        {
            ragdollGrabbableObject.ragdoll.attachedTo = null;
            ragdollGrabbableObject.ragdoll.attachedLimb = null;
            ragdollGrabbableObject.transform.SetParent(StartOfRound.Instance.propsContainer);
            ragdollGrabbableObject.ragdoll.matchPositionExactly = false;
            ragdollGrabbableObject.ragdoll.lerpBeforeMatchingPosition = false;
            ragdollGrabbableObject.ragdoll.gameObject.SetActive(false);
            ragdollGrabbableObject.ragdoll.deactivated = true;

            ragdollGrabbableObject.isHeld = false;
            ragdollGrabbableObject.parentObject = null;
            ragdollGrabbableObject.hasHitGround = false;

            this.idPlayerHolder = -1;
        }

        public float GetWeight()
        {
            return ragdollGrabbableObject.itemProperties.weight;
        }

        public DeadBodyInfo? GetDeadBodyInfo()
        {
            return ragdollGrabbableObject?.ragdoll;
        }

        public bool IsRagdollBodyHeldByPlayer(int idPlayer)
        {
            return idPlayer == idPlayerHolder && IsRagdollBodyHeld();
        }

        public bool IsRagdollBodyHeld()
        {
            return ragdollGrabbableObject.isHeld;
        }

        public PlayerControllerB GetPlayerHolder()
        {
            return StartOfRound.Instance.allPlayerScripts[idPlayerHolder];
        }
    }
}

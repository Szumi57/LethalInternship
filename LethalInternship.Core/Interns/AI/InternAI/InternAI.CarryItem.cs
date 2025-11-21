using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public HeldItems HeldItems { get; set; } = new HeldItems();

        private Coroutine grabObjectCoroutine = null!;
        private Coroutine? dropAllObjectsCoroutine = null!;
        private bool dropAllObjectsCoroutineRunning = false;

        /// <summary>
        /// Is the intern holding an item ?
        /// </summary>
        /// <returns>I mean come on</returns>
        public bool AreHandsFree()
        {
            return !HeldItems.IsHoldingAnItem();
        }

        public bool AreFreeSlotsAvailable()
        {
            return HeldItems.GetFreeSlots() > 0;
        }

        public bool CanHoldItem(GrabbableObject grabbableObject)
        {
            if (grabbableObject.itemProperties.twoHanded)
            {
                return !HeldItems.IsHoldingTwoHandedItem() && AreFreeSlotsAvailable();
            }

            return AreFreeSlotsAvailable();
        }

        public bool IsHoldingItem(GrabbableObject grabbableObject)
        {
            return HeldItems.IsHoldingItem(grabbableObject);
        }

        public void UpdateItemOffsetsWhileHeld()
        {
            foreach (var item in HeldItems.Items)
            {
                if (item.GrabbableObject == null)
                {
                    continue;
                }

                item.GrabbableObject.transform.localPosition = item.GrabbableObject.itemProperties.positionOffset;
                item.GrabbableObject.transform.localEulerAngles = item.GrabbableObject.itemProperties.rotationOffset;
            }
        }

        public bool IsHoldingTwoHandedItem()
        {
            return HeldItems.IsHoldingTwoHandedItem();
        }

        private bool ShouldUseTwoHandedHoldAnim()
        {
            return HeldItems.IsHoldingTwoHandedItem() && HeldItems.Items.Count > 1;
        }

        #region Grab item RPC

        /// <summary>
        /// Server side, call clients to make the intern grab item on their side to sync everyone
        /// </summary>
        /// <param name="networkObjectReference">Item reference over the network</param>
        [ServerRpc(RequireOwnership = false)]
        public void GrabItemServerRpc(NetworkObjectReference networkObjectReference, bool itemGiven)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            if (!itemGiven)
            {
                if (!IsGrabbableObjectGrabbable(grabbableObject))
                {
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} grabbableObject {grabbableObject} not grabbable");
                    return;
                }
            }

            GrabItemClientRpc(networkObjectReference);
        }

        /// <summary>
        /// Client side, make the intern grab item
        /// </summary>
        /// <param name="networkObjectReference">Item reference over the network</param>
        [ClientRpc]
        private void GrabItemClientRpc(NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GrabItem for InternAI {InternId}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            if (HeldItems.IsHoldingItem(grabbableObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} cannot grab already held item {grabbableObject} on client #{NetworkManager.LocalClientId}");
                return;
            }

            GrabItem(grabbableObject);
        }

        /// <summary>
        /// Make the intern grab an item like an enemy would, but update the body (<c>PlayerControllerB</c>) too.
        /// </summary>
        /// <param name="grabbableObject">Item to grab</param>
        private void GrabItem(GrabbableObject grabbableObject)
        {
            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} try to grab item {grabbableObject} on client #{NetworkManager.LocalClientId}");
            HeldItems.HoldItem(grabbableObject);

            grabbableObject.GrabItemFromEnemy(this);
            grabbableObject.parentObject = NpcController.Npc.serverItemHolder;
            grabbableObject.playerHeldBy = NpcController.Npc;
            grabbableObject.isHeld = true;
            grabbableObject.hasHitGround = false;
            grabbableObject.isInFactory = NpcController.Npc.isInsideFactory;
            grabbableObject.EquipItem();

            NpcController.Npc.isHoldingObject = true;
            NpcController.Npc.currentlyHeldObjectServer = grabbableObject;
            NpcController.Npc.twoHanded = ShouldUseTwoHandedHoldAnim();
            NpcController.Npc.twoHandedAnimation = HeldItems.IsHoldingTwoHandedItemWithAnimation();
            NpcController.Npc.carryWeight += Mathf.Clamp(grabbableObject.itemProperties.weight - 1f, 0f, 10f);
            NpcController.GrabbedObjectValidated = true;
            if (grabbableObject.itemProperties.grabSFX != null)
            {
                NpcController.Npc.itemAudio.PlayOneShot(grabbableObject.itemProperties.grabSFX, 1f);
            }

            // animations
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABINVALIDATED, false);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABVALIDATED, false);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, false);
            NpcController.Npc.playerBodyAnimator.ResetTrigger(Const.PLAYER_ANIMATION_TRIGGER_THROW);
            SetSpecialGrabAnimationBool(true, grabbableObject);

            if (grabObjectCoroutine != null)
            {
                StopCoroutine(grabObjectCoroutine);
            }
            grabObjectCoroutine = StartCoroutine(GrabAnimationCoroutine());

            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Grabbed item {grabbableObject} on client #{NetworkManager.LocalClientId}");
        }

        /// <summary>
        /// Coroutine for the grab animation
        /// </summary>
        /// <returns></returns>
        private IEnumerator GrabAnimationCoroutine()
        {
            GrabbableObject? lastPickedUpItem = HeldItems.GetLastPickedUpItem();
            if (lastPickedUpItem != null)
            {
                float grabAnimationTime = lastPickedUpItem.itemProperties.grabAnimationTime > 0f ? lastPickedUpItem.itemProperties.grabAnimationTime : 0.4f;
                yield return new WaitForSeconds(grabAnimationTime - 0.2f);
                NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABVALIDATED, true);
                NpcController.Npc.isGrabbingObjectAnimation = false;
            }
            yield break;
        }

        /// <summary>
        /// Set the animation of body to something special if the item has a special grab animation.
        /// </summary>
        /// <param name="setBool">Activate or deactivate special animation</param>
        /// <param name="item">Item that has the special grab animation</param>
        private void SetSpecialGrabAnimationBool(bool setBool, GrabbableObject? item)
        {
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRAB, setBool);
            if (item != null
                && !string.IsNullOrEmpty(item.itemProperties.grabAnim))
            {
                try
                {
                    NpcController.SetAnimationBoolForItem(item.itemProperties.grabAnim, setBool);
                    NpcController.Npc.playerBodyAnimator.SetBool(item.itemProperties.grabAnim, setBool);
                }
                catch (Exception)
                {
                    PluginLoggerHook.LogError?.Invoke("An item tried to set an animator bool which does not exist: " + item.itemProperties.grabAnim);
                }
            }
        }

        #endregion

        #region Drop item RPC

        /// <summary>
        /// Make the intern drop his item like an enemy, but update the body (<c>PlayerControllerB</c>) too.
        /// </summary>
        public void DropItem(GrabbableObject itemToDrop)
        {
            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Try to drop item on client #{NetworkManager.LocalClientId}");
            if (!HeldItems.IsHoldingItem(itemToDrop))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} Try to drop not held item on client #{NetworkManager.LocalClientId}");
                return;
            }

            bool placeObject = false;
            Vector3 placePosition = default;
            NetworkObject parentObjectTo = null!;
            bool matchRotationOfParent = true;
            Vector3 vector;
            NetworkObject physicsRegionOfDroppedObject = itemToDrop.GetPhysicsRegionOfDroppedObject(NpcController.Npc, out vector);
            if (physicsRegionOfDroppedObject != null)
            {
                placePosition = vector;
                parentObjectTo = physicsRegionOfDroppedObject;
                placeObject = true;
                matchRotationOfParent = false;
            }

            if (placeObject)
            {
                if (parentObjectTo == null)
                {
                    if (NpcController.Npc.isInElevator)
                    {
                        placePosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(placePosition);
                    }
                    else
                    {
                        placePosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(placePosition);
                    }
                    int floorYRot2 = (int)transform.localEulerAngles.y;

                    // on client
                    SetObjectAsNoLongerHeld(itemToDrop,
                                            NpcController.Npc.isInElevator,
                                            NpcController.Npc.isInHangarShipRoom,
                                            placePosition,
                                            floorYRot2);
                    // for other clients
                    SetObjectAsNoLongerHeldServerRpc(new DropItemNetworkSerializable()
                    {
                        DroppedInElevator = NpcController.Npc.isInElevator,
                        DroppedInShipRoom = NpcController.Npc.isInHangarShipRoom,
                        FloorYRot = floorYRot2,
                        GrabbedObject = itemToDrop.NetworkObject,
                        TargetFloorPosition = placePosition
                    });
                }
                else
                {
                    // on client
                    PlaceGrabbableObject(itemToDrop, parentObjectTo.transform, placePosition, matchRotationOfParent);

                    // for other clients
                    PlaceGrabbableObjectServerRpc(new PlaceItemNetworkSerializable()
                    {
                        GrabbedObject = itemToDrop.NetworkObject,
                        MatchRotationOfParent = matchRotationOfParent,
                        ParentObject = parentObjectTo,
                        PlacePositionOffset = placePosition
                    });
                }
            }
            else
            {
                bool droppedInElevator = NpcController.Npc.isInElevator;
                Vector3 targetFloorPosition;
                if (!NpcController.Npc.isInElevator)
                {
                    Vector3 vector2;
                    if (itemToDrop.itemProperties.allowDroppingAheadOfPlayer)
                    {
                        vector2 = DropItemAheadOfPlayer(itemToDrop, NpcController.Npc);
                    }
                    else
                    {
                        vector2 = itemToDrop.GetItemFloorPosition(default);
                    }
                    if (!NpcController.Npc.playersManager.shipBounds.bounds.Contains(vector2))
                    {
                        targetFloorPosition = NpcController.Npc.playersManager.propsContainer.InverseTransformPoint(vector2);
                    }
                    else
                    {
                        droppedInElevator = true;
                        targetFloorPosition = NpcController.Npc.playersManager.elevatorTransform.InverseTransformPoint(vector2);
                    }
                }
                else
                {
                    Vector3 vector2 = itemToDrop.GetItemFloorPosition(default);
                    if (!NpcController.Npc.playersManager.shipBounds.bounds.Contains(vector2))
                    {
                        droppedInElevator = false;
                        targetFloorPosition = NpcController.Npc.playersManager.propsContainer.InverseTransformPoint(vector2);
                    }
                    else
                    {
                        targetFloorPosition = NpcController.Npc.playersManager.elevatorTransform.InverseTransformPoint(vector2);
                    }
                }
                int floorYRot = (int)transform.localEulerAngles.y;

                // on client
                SetObjectAsNoLongerHeld(itemToDrop,
                                        droppedInElevator,
                                        NpcController.Npc.isInHangarShipRoom,
                                        targetFloorPosition,
                                        floorYRot);

                // for other clients
                SetObjectAsNoLongerHeldServerRpc(new DropItemNetworkSerializable()
                {
                    DroppedInElevator = droppedInElevator,
                    DroppedInShipRoom = NpcController.Npc.isInHangarShipRoom,
                    FloorYRot = floorYRot,
                    GrabbedObject = itemToDrop.NetworkObject,
                    TargetFloorPosition = targetFloorPosition
                });
            }
        }

        public void DropFirstPickedUpItem()
        {
            GrabbableObject? firstPickedUpItem = HeldItems.GetFirstPickedUpItem();
            if (firstPickedUpItem == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} DropFirstPickedUpItem: Failed, no item found !");
                return;
            }

            DropItem(firstPickedUpItem);
        }

        public void DropLastPickedUpItem()
        {
            GrabbableObject? lastPickedUpItem = HeldItems.GetLastPickedUpItem();
            if (lastPickedUpItem == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} DropLastPickedUpItem: Failed, no item found !");
                return;
            }

            DropItem(lastPickedUpItem);
        }

        public void DropTwoHandItem()
        {
            GrabbableObject? twoHandItem = HeldItems.GetTwoHandItem();
            if (twoHandItem == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} DropTwoHandItem: Failed, no item found !");
                return;
            }

            DropItem(twoHandItem);
        }

        public void DropAllItems(bool waitBetweenItems = true)
        {
            if (waitBetweenItems)
            {
                if (!dropAllObjectsCoroutineRunning)
                {
                    if (dropAllObjectsCoroutine != null)
                    {
                        StopCoroutine(dropAllObjectsCoroutine);
                    }
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} DropAllItems: Starting coroutine");
                    dropAllObjectsCoroutine = StartCoroutine(DropAllItemsCoroutine());
                }
            }
            else
            {
                while (!this.AreHandsFree())
                {
                    PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} DropAllItems waitBetweenItems false, dropping item");
                    DropLastPickedUpItem();
                }
            }
        }

        private IEnumerator DropAllItemsCoroutine()
        {
            dropAllObjectsCoroutineRunning = true;
            while (!this.AreHandsFree())
            {
                GrabbableObject? lastPickedUpItem = HeldItems.GetLastPickedUpItem();
                if (lastPickedUpItem == null)
                {
                    PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} DropAllItemsCoroutine: Failed, no item found !");
                    yield break;
                }

                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} dropping {lastPickedUpItem}");
                DropItem(lastPickedUpItem);
                yield return new WaitForSeconds(0.4f);
            }
            dropAllObjectsCoroutineRunning = false;
        }

        private Vector3 DropItemAheadOfPlayer(GrabbableObject grabbableObject, PlayerControllerB player)
        {
            Vector3 vector;
            Ray ray = new Ray(transform.position + Vector3.up * 0.4f, player.gameplayCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 1.7f, 268438273, QueryTriggerInteraction.Ignore))
            {
                vector = ray.GetPoint(Mathf.Clamp(hit.distance - 0.3f, 0.01f, 2f));
            }
            else
            {
                vector = ray.GetPoint(1.7f);
            }
            Vector3 itemFloorPosition = grabbableObject.GetItemFloorPosition(vector);
            if (itemFloorPosition == vector)
            {
                itemFloorPosition = grabbableObject.GetItemFloorPosition(default);
            }
            return itemFloorPosition;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetObjectAsNoLongerHeldServerRpc(DropItemNetworkSerializable dropItemNetworkSerializable)
        {
            NetworkObject networkObject;
            if (dropItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null))
            {
                SetObjectAsNoLongerHeldClientRpc(dropItemNetworkSerializable);
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke($"Intern {NpcController.Npc.playerUsername} on client #{NetworkManager.LocalClientId} (server) drop item : Object was not thrown because it does not exist on the server.");
            }
        }

        [ClientRpc]
        private void SetObjectAsNoLongerHeldClientRpc(DropItemNetworkSerializable dropItemNetworkSerializable)
        {
            if (!dropItemNetworkSerializable.GrabbedObject.TryGet(out NetworkObject networkObject, null))
            {
                PluginLoggerHook.LogError?.Invoke($"SetObjectAsNoLongerHeldClientRpc {NpcController.Npc.playerUsername} no networkObject found in dropItemNetworkSerializable ! on client #{NetworkManager.LocalClientId}");
                return;
            }

            GrabbableObject? itemToDrop = networkObject.GetComponent<GrabbableObject>();
            if (itemToDrop == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SetObjectAsNoLongerHeldClientRpc {NpcController.Npc.playerUsername} no GrabbableObject found in networkObject ! on client #{NetworkManager.LocalClientId}");
                return;
            }

            if (!HeldItems.IsHoldingItem(itemToDrop))
            {
                PluginLoggerHook.LogWarning?.Invoke($"{NpcController.Npc.playerUsername} held item already dropped, on client #{NetworkManager.LocalClientId}");
                return;
            }

            SetObjectAsNoLongerHeld(itemToDrop,
                                    dropItemNetworkSerializable.DroppedInElevator,
                                    dropItemNetworkSerializable.DroppedInShipRoom,
                                    dropItemNetworkSerializable.TargetFloorPosition,
                                    dropItemNetworkSerializable.FloorYRot);
        }

        private void SetObjectAsNoLongerHeld(GrabbableObject grabbableObject,
                                             bool droppedInElevator,
                                             bool droppedInShipRoom,
                                             Vector3 targetFloorPosition,
                                             int floorYRot = -1)
        {
            grabbableObject.heldByPlayerOnServer = false;
            grabbableObject.parentObject = null;
            if (droppedInElevator)
            {
                grabbableObject.transform.SetParent(NpcController.Npc.playersManager.elevatorTransform, true);
            }
            else
            {
                grabbableObject.transform.SetParent(NpcController.Npc.playersManager.propsContainer, true);
            }

            NpcController.Npc.SetItemInElevator(droppedInShipRoom, droppedInElevator, grabbableObject);
            grabbableObject.EnablePhysics(true);
            grabbableObject.EnableItemMeshes(true);
            grabbableObject.isHeld = false;
            grabbableObject.isPocketed = false;
            grabbableObject.fallTime = 0f;
            grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
            grabbableObject.targetFloorPosition = targetFloorPosition;
            grabbableObject.floorYRot = floorYRot;

            EndDropItem(grabbableObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlaceGrabbableObjectServerRpc(PlaceItemNetworkSerializable placeItemNetworkSerializable)
        {
            NetworkObject networkObject;
            NetworkObject networkObject2;
            if (placeItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null)
                && placeItemNetworkSerializable.ParentObject.TryGet(out networkObject2, null))
            {
                PlaceGrabbableObjectClientRpc(placeItemNetworkSerializable);
                return;
            }

            NetworkObject networkObject3;
            if (!placeItemNetworkSerializable.GrabbedObject.TryGet(out networkObject3, null))
            {
                PluginLoggerHook.LogError?.Invoke($"Object placement not synced to clients, missing reference to a network object: placing object with id: {placeItemNetworkSerializable.GrabbedObject.NetworkObjectId}; intern {NpcController.Npc.playerUsername}");
                return;
            }
            NetworkObject networkObject4;
            if (!placeItemNetworkSerializable.ParentObject.TryGet(out networkObject4, null))
            {
                PluginLoggerHook.LogError?.Invoke($"Object placement not synced to clients, missing reference to a network object: parent object with id: {placeItemNetworkSerializable.ParentObject.NetworkObjectId}; intern {NpcController.Npc.playerUsername}");
            }
        }

        [ClientRpc]
        private void PlaceGrabbableObjectClientRpc(PlaceItemNetworkSerializable placeItemNetworkSerializable)
        {
            NetworkObject networkObject;
            if (placeItemNetworkSerializable.GrabbedObject.TryGet(out networkObject, null))
            {
                GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
                NetworkObject networkObject2;
                if (placeItemNetworkSerializable.ParentObject.TryGet(out networkObject2, null))
                {
                    PlaceGrabbableObject(grabbableObject,
                                              networkObject2.transform,
                                              placeItemNetworkSerializable.PlacePositionOffset,
                                              placeItemNetworkSerializable.MatchRotationOfParent);
                }
                else
                {
                    PluginLoggerHook.LogError?.Invoke($"Reference to parent object when placing was missing. object: {grabbableObject} placed by intern #{NpcController.Npc.playerUsername}");
                }
            }
            else
            {
                PluginLoggerHook.LogError?.Invoke("The server did not have a reference to the held object (when attempting to PLACE object on client.)");
            }
        }

        private void PlaceGrabbableObject(GrabbableObject placeObject, Transform parentObject, Vector3 positionOffset, bool matchRotationOfParent)
        {
            if (!HeldItems.IsHoldingItem(placeObject))
            {
                PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} held item already placed, on client #{NetworkManager.LocalClientId}");
                return;
            }

            PlayerPhysicsRegion componentInChildren = parentObject.GetComponentInChildren<PlayerPhysicsRegion>();
            if (componentInChildren != null && componentInChildren.allowDroppingItems)
            {
                parentObject = componentInChildren.physicsTransform;
            }
            placeObject.EnablePhysics(true);
            placeObject.EnableItemMeshes(true);
            placeObject.isHeld = false;
            placeObject.isPocketed = false;
            placeObject.heldByPlayerOnServer = false;
            NpcController.Npc.SetItemInElevator(NpcController.Npc.isInHangarShipRoom, NpcController.Npc.isInElevator, placeObject);
            placeObject.parentObject = null;
            placeObject.transform.SetParent(parentObject, true);
            placeObject.startFallingPosition = placeObject.transform.localPosition;
            placeObject.transform.localScale = placeObject.originalScale;
            placeObject.transform.localPosition = positionOffset;
            placeObject.targetFloorPosition = positionOffset;
            if (!matchRotationOfParent)
            {
                placeObject.fallTime = 0f;
            }
            else
            {
                placeObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                placeObject.fallTime = 1.1f;
            }
            placeObject.OnPlaceObject();

            EndDropItem(placeObject);
        }

        private void EndDropItem(GrabbableObject grabbableObject)
        {
            grabbableObject.DiscardItem();
            SetSpecialGrabAnimationBool(false, grabbableObject);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, true);
            NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_THROW);

            InternManager.Instance.AddToDictJustDroppedItems(grabbableObject);

            HeldItems.DropItem(grabbableObject);
            NpcController.Npc.isHoldingObject = HeldItems.IsHoldingAnItem();
            NpcController.Npc.currentlyHeldObjectServer = null;
            NpcController.Npc.twoHanded = ShouldUseTwoHandedHoldAnim();
            NpcController.Npc.twoHandedAnimation = HeldItems.IsHoldingTwoHandedItemWithAnimation();
            NpcController.GrabbedObjectValidated = false;

            float weightToLose = grabbableObject.itemProperties.weight - 1f < 0f ? 0f : grabbableObject.itemProperties.weight - 1f;
            NpcController.Npc.carryWeight = Mathf.Clamp(NpcController.Npc.carryWeight - weightToLose, 1f, 10f);

            SyncBatteryIntern(grabbableObject, (int)(grabbableObject.insertedBattery.charge * 100f));
            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} dropped {grabbableObject}, on client #{NetworkManager.LocalClientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncBatteryInternServerRpc(NetworkObjectReference networkObjectReferenceGrabbableObject, int charge)
        {
            SyncBatteryInternClientRpc(networkObjectReferenceGrabbableObject, charge);
        }

        [ClientRpc]
        private void SyncBatteryInternClientRpc(NetworkObjectReference networkObjectReferenceGrabbableObject, int charge)
        {
            if (!networkObjectReferenceGrabbableObject.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"SyncBatteryInternClientRpc : Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"SyncBatteryInternClientRpc : Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            SyncBatteryIntern(grabbableObject, charge);
        }

        private void SyncBatteryIntern(GrabbableObject grabbableObject, int charge)
        {
            float num = charge / 100f;
            grabbableObject.insertedBattery = new Battery(num <= 0f, num);
            grabbableObject.ChargeBatteries();
        }

        #endregion

        #region Give item to intern RPC

        [ServerRpc(RequireOwnership = false)]
        public void GiveItemToInternServerRpc(ulong playerClientIdGiver, NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternServerRpc for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternServerRpc for InternAI {InternId} {NpcController.Npc.playerUsername}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            GiveItemToInternClientRpc(playerClientIdGiver, networkObjectReference);
        }

        [ClientRpc]
        private void GiveItemToInternClientRpc(ulong playerClientIdGiver, NetworkObjectReference networkObjectReference)
        {
            if (!networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternClientRpc for InternAI {InternId}: Failed to get network object from network object reference (Grab item RPC)");
                return;
            }

            GrabbableObject grabbableObject = networkObject.GetComponent<GrabbableObject>();
            if (grabbableObject == null)
            {
                PluginLoggerHook.LogError?.Invoke($"{NpcController.Npc.playerUsername} GiveItemToInternClientRpc for InternAI {InternId}: Failed to get GrabbableObject component from network object (Grab item RPC)");
                return;
            }

            GiveItemToIntern(playerClientIdGiver, grabbableObject);
        }

        private void GiveItemToIntern(ulong playerClientIdGiver, GrabbableObject grabbableObject)
        {
            PluginLoggerHook.LogDebug?.Invoke($"GiveItemToIntern playerClientIdGiver {playerClientIdGiver}, localPlayerController {StartOfRound.Instance.localPlayerController.playerClientId}");
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerClientIdGiver];

            // Discard for player
            if (player.playerClientId == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                PlayerControllerBHook.SetSpecialGrabAnimationBool_ReversePatch?.Invoke(player, false, player.currentlyHeldObjectServer);
                player.playerBodyAnimator.SetBool("cancelHolding", true);
                player.playerBodyAnimator.SetTrigger("Throw");
                HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
                HUDManager.Instance.holdingTwoHandedItem.enabled = false;
                HUDManager.Instance.ClearControlTips();
            }

            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                if (player.ItemSlots[i] == grabbableObject)
                {
                    player.ItemSlots[i] = null;
                }
            }

            grabbableObject.EnablePhysics(true);
            grabbableObject.EnableItemMeshes(true);
            grabbableObject.parentObject = null;
            grabbableObject.heldByPlayerOnServer = false;
            grabbableObject.DiscardItem();

            player.isHoldingObject = false;
            player.currentlyHeldObjectServer = null;
            player.twoHanded = false;
            player.twoHandedAnimation = false;

            float weightToLose = grabbableObject.itemProperties.weight - 1f < 0f ? 0f : grabbableObject.itemProperties.weight - 1f;
            player.carryWeight = Mathf.Clamp(player.carryWeight - weightToLose, 1f, 10f);

            SyncBatteryInternServerRpc(grabbableObject.NetworkObject, (int)(grabbableObject.insertedBattery.charge * 100f));

            // Intern grab item
            GrabItem(grabbableObject);
        }

        #endregion
    }
}

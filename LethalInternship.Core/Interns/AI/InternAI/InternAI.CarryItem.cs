using GameNetcodeStuff;
using LethalInternship.Core.Interns.AI.Items;
using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Hooks.PlayerControllerBHooks;
using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.NetworkSerializers;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Core.Interns.AI
{
    public partial class InternAI
    {
        public HeldItems HeldItems { get; set; } = new HeldItems();

        private Transform WeaponHolderTransform = null!;
        private bool HasWeaponAsPrimary = false;

        private Coroutine grabObjectCoroutine = null!;
        private Coroutine? dropAllObjectsCoroutine = null!;
        private bool dropAllObjectsCoroutineRunning = false;
        private HeldItem heldItemTemp = null!;

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
            return PluginRuntimeProvider.Context.Config.NbMaxCanCarry - HeldItems.NbHeldItems > 0;
        }

        public bool CanHoldItem(GrabbableObject grabbableObject)
        {
            heldItemTemp = new HeldItem(grabbableObject);
            if (heldItemTemp.IsWeapon
                && PluginRuntimeProvider.Context.Config.CanUseWeapons
                && !HeldItems.IsHoldingWeaponAsWeapon())
            {
                return true;
            }

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

        private bool ShouldUseTwoHandedHoldAnim(bool ignoreHeldWeapon = true)
        {
            return HeldItems.IsHoldingTwoHandedAnimationItem(ignoreHeldWeapon) || HeldItems.NbHeldItems > 1;
        }

        //Quaternion lastLocalRotation = Quaternion.identity;
        //Vector3 lastLocalPosition = Vector3.zero;
        public void UpdateItemRotation(GrabbableObject grabbableObject)
        {
            //if (PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition.IsPressed()) { EquipWeaponAsPrimary(); }
            //if (PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.IsPressed()) { UnequipWeaponAsPrimary(); }

            if (HasWeaponAsPrimary)
            {
                return;
            }
            if (grabbableObject != HeldItems.GetHeldWeapon())
            {
                return;
            }

            // Init position and rotation
            WeaponHolderTransform.rotation = WeaponHolderTransform.parent.rotation;
            grabbableObject.transform.position = grabbableObject.parentObject.position;
            grabbableObject.transform.rotation = grabbableObject.parentObject.rotation;
            if (grabbableObject.transform.parent != WeaponHolderTransform.transform) // Important
            {
                grabbableObject.transform.SetParent(WeaponHolderTransform.transform);
            }

            //// Use this to rotate manually and find the localRotation to save
            //// --------------------------------------------------------------
            //grabbableObject.transform.localRotation = (lastLocalRotation == Quaternion.identity) ? grabbableObject.transform.localRotation : lastLocalRotation;
            //grabbableObject.transform.localPosition = lastLocalPosition;
            //// Local rotation
            //if (PluginRuntimeProvider.Context.InputActionsInstance.ManageIntern.IsPressed()) { grabbableObject.transform.Rotate(1f, 0f, 0f, Space.Self); }
            //if (PluginRuntimeProvider.Context.InputActionsInstance.GrabIntern.IsPressed()) { grabbableObject.transform.Rotate(0f, 1f, 0f, Space.Self); }
            //if (PluginRuntimeProvider.Context.InputActionsInstance.ReleaseInterns.IsPressed()) { grabbableObject.transform.Rotate(0f, 0f, 1f, Space.Self); }
            //lastLocalRotation = grabbableObject.transform.localRotation;
            //// Local position
            //if (PluginRuntimeProvider.Context.InputActionsInstance.MakeInternLookAtPosition.IsPressed()) { grabbableObject.transform.Translate(0f, 0.05f, 0f, Space.World); }
            //if (PluginRuntimeProvider.Context.InputActionsInstance.ChangeSuitIntern.IsPressed()) { grabbableObject.transform.Translate(0f, -0.05f, 0f, Space.World); }
            //lastLocalPosition = grabbableObject.transform.localPosition;
            //PluginLoggerHook.LogDebug?.Invoke($"lastLocalRotation = {lastLocalRotation}, lastLocalPosition = {lastLocalPosition}");
            //// --------------------------------------------------------------

            // Apply position and rotation for each weapon
            if (grabbableObject.name.Contains("ShovelItem")
                || grabbableObject.name.Contains("StopSign")
                || grabbableObject.name.Contains("YieldSign"))
            {
                grabbableObject.transform.localRotation = new Quaternion(0.18501f, 0.68813f, 0.68882f, 0.13332f);
            }
            else if (grabbableObject.name.Contains("ShotgunItem"))
            {
                grabbableObject.transform.localRotation = new Quaternion(0.10351f, 0.73728f, -0.63055f, -0.21936f);
                grabbableObject.transform.localPosition = new Vector3(0.00f, 0.10f, -0.07f);
            }
            else if (grabbableObject.name.Contains("KnifeItem"))
            {
                grabbableObject.transform.localRotation = new Quaternion(0.20982f, 0.64721f, -0.21568f, 0.70041f);
                grabbableObject.transform.localPosition = new Vector3(0.00f, -0.30f, -0.01f);
            }
            else if (grabbableObject.name.Contains("PatcherGunItem"))
            {
                grabbableObject.transform.localRotation = new Quaternion(0.98072f, 0.02271f, -0.11774f, -0.15432f);
                grabbableObject.transform.localPosition = new Vector3(0.00f, 0.30f, -0.01f);
            }
        }

        public void EquipWeaponAsPrimary()
        {
            if (HasWeaponAsPrimary)
            {
                return;
            }

            GrabbableObject? weapon = HeldItems.GetHeldWeapon();
            if (weapon == null)
            {
                return;
            }

            weapon.parentObject = NpcController.Npc.serverItemHolder;
            NpcController.Npc.isHoldingObject = true;
            NpcController.Npc.twoHanded = IsHoldingTwoHandedItem();
            NpcController.Npc.twoHandedAnimation = ShouldUseTwoHandedHoldAnim(ignoreHeldWeapon: false);
            HeldItems.ShowHideAllItemsMeshes(show: false, includeHeldWeapon: false);

            // Animations
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRAB, true);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, false);
            SetSpecialGrabAnimationBool(true, weapon.itemProperties.grabAnim);

            Npc.playerBodyAnimator.ResetTrigger("SwitchHoldAnimationTwoHanded");
            Npc.playerBodyAnimator.SetTrigger("SwitchHoldAnimationTwoHanded");
            Npc.playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");
            Npc.playerBodyAnimator.SetTrigger("SwitchHoldAnimation");

            HasWeaponAsPrimary = true;
        }

        public void UnequipWeaponAsPrimary()
        {
            if (!HasWeaponAsPrimary)
            {
                return;
            }

            GrabbableObject? weapon = HeldItems.GetHeldWeapon();
            if (weapon == null)
            {
                return;
            }

            weapon.parentObject = WeaponHolderTransform;
            NpcController.Npc.isHoldingObject = HeldItems.IsHoldingAnItem();
            NpcController.Npc.twoHanded = IsHoldingTwoHandedItem();
            NpcController.Npc.twoHandedAnimation = ShouldUseTwoHandedHoldAnim();
            HeldItems.ShowHideAllItemsMeshes(show: true);

            // Animations
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRAB, NpcController.Npc.isHoldingObject);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, !NpcController.Npc.isHoldingObject);
            if (NpcController.Npc.twoHandedAnimation)
            {
                SetSpecialGrabAnimationBool(true, "HoldLung");
            }

            Npc.playerBodyAnimator.ResetTrigger("SwitchHoldAnimationTwoHanded");
            Npc.playerBodyAnimator.SetTrigger("SwitchHoldAnimationTwoHanded");
            Npc.playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");
            Npc.playerBodyAnimator.SetTrigger("SwitchHoldAnimation");

            HasWeaponAsPrimary = false;
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
            grabbableObject.parentObject = HeldItems.IsHoldingItemAsWeapon(grabbableObject) ? WeaponHolderTransform : NpcController.Npc.serverItemHolder;
            grabbableObject.playerHeldBy = NpcController.Npc;
            grabbableObject.isHeld = true;
            grabbableObject.hasHitGround = false;
            grabbableObject.isInFactory = NpcController.Npc.isInsideFactory;
            grabbableObject.EquipItem();

            NpcController.Npc.isHoldingObject = HeldItems.IsHoldingAnItem();
            NpcController.Npc.currentlyHeldObjectServer = HeldItems.GetCurrentlyHeldItem(ignoreWeapon: PluginRuntimeProvider.Context.Config.CanUseWeapons);
            NpcController.Npc.twoHanded = IsHoldingTwoHandedItem();
            NpcController.Npc.twoHandedAnimation = ShouldUseTwoHandedHoldAnim();
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

            if (HasWeaponAsPrimary)
            {
                HeldItems.ShowHideAllItemsMeshes(show: false, includeHeldWeapon: false);
            }
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRAB, NpcController.Npc.isHoldingObject);
            if (NpcController.Npc.twoHandedAnimation)
            {
                SetSpecialGrabAnimationBool(true, "HoldLung");
            }
            Npc.playerBodyAnimator.ResetTrigger("SwitchHoldAnimationTwoHanded");
            Npc.playerBodyAnimator.SetTrigger("SwitchHoldAnimationTwoHanded");
            Npc.playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");
            Npc.playerBodyAnimator.SetTrigger("SwitchHoldAnimation");

            if (grabObjectCoroutine != null)
            {
                StopCoroutine(grabObjectCoroutine);
            }
            grabObjectCoroutine = StartCoroutine(GrabAnimationCoroutine(grabbableObject));

            PluginLoggerHook.LogDebug?.Invoke($"{NpcController.Npc.playerUsername} Grabbed item {grabbableObject} on client #{NetworkManager.LocalClientId}");
        }

        /// <summary>
        /// Coroutine for the grab animation
        /// </summary>
        /// <returns></returns>
        private IEnumerator GrabAnimationCoroutine(GrabbableObject lastPickedUpItem)
        {
            float grabAnimationTime = lastPickedUpItem.itemProperties.grabAnimationTime > 0f ? lastPickedUpItem.itemProperties.grabAnimationTime : 0.4f;
            yield return new WaitForSeconds(grabAnimationTime - 0.2f);
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRABVALIDATED, true);
            NpcController.Npc.isGrabbingObjectAnimation = false;
            yield break;
        }

        /// <summary>
        /// Set the animation of body to something special if the item has a special grab animation.
        /// </summary>
        /// <param name="setBool">Activate or deactivate special animation</param>
        /// <param name="item">Item that has the special grab animation</param>
        private void SetSpecialGrabAnimationBool(bool setBool, string grabAnim)
        {
            try
            {
                NpcController.SetAnimationBoolForItem(grabAnim, setBool);
            }
            catch (Exception)
            {
                PluginLoggerHook.LogError?.Invoke("An item tried to set an animator bool which does not exist: " + grabAnim);
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

        public GrabbableObject? ChooseFirstPickedUpItem(EnumOptionsGetItems options)
        {
            switch (options)
            {
                case EnumOptionsGetItems.All:
                    for (int i = 0; i < HeldItems.Items.Count; i++)
                    {
                        var item = HeldItems.Items[i];
                        if (item.GrabbableObject == null)
                        {
                            continue;
                        }
                        return item.GrabbableObject;
                    }
                    return null;

                case EnumOptionsGetItems.IgnoreWeapon:
                    for (int i = 0; i < HeldItems.Items.Count; i++)
                    {
                        var item = HeldItems.Items[i];
                        if (item.GrabbableObject == null)
                        {
                            continue;
                        }

                        if (HeldItems.IsHoldingItemAsWeapon(item.GrabbableObject))
                        {
                            continue;
                        }
                        return item.GrabbableObject;
                    }
                    return null;

                case EnumOptionsGetItems.ChooseWeaponLast:
                    for (int i = 0; i < HeldItems.Items.Count; i++)
                    {
                        var item = HeldItems.Items[i];
                        if (item.GrabbableObject == null)
                        {
                            continue;
                        }

                        if (HeldItems.IsHoldingItemAsWeapon(item.GrabbableObject))
                        {
                            if (i == HeldItems.Items.Count - 1)
                            {
                                return item.GrabbableObject;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        return item.GrabbableObject;
                    }
                    return null;
                default:
                    return null;
            }
        }

        public GrabbableObject? ChooseLastPickedUpItem(EnumOptionsGetItems options)
        {
            switch (options)
            {
                case EnumOptionsGetItems.All:
                    for (int i = HeldItems.Items.Count - 1; i >= 0; i--)
                    {
                        var item = HeldItems.Items[i];
                        if (item.GrabbableObject == null)
                        {
                            continue;
                        }
                        return item.GrabbableObject;
                    }
                    return null;

                case EnumOptionsGetItems.IgnoreWeapon:
                    for (int i = HeldItems.Items.Count - 1; i >= 0; i--)
                    {
                        var item = HeldItems.Items[i];
                        if (item.GrabbableObject == null)
                        {
                            continue;
                        }

                        if (HeldItems.IsHoldingItemAsWeapon(item.GrabbableObject))
                        {
                            continue;
                        }
                        return item.GrabbableObject;
                    }
                    return null;

                case EnumOptionsGetItems.ChooseWeaponLast:
                    for (int i = HeldItems.Items.Count - 1; i >= 0; i--)
                    {
                        var item = HeldItems.Items[i];
                        if (item.GrabbableObject == null)
                        {
                            continue;
                        }

                        if (HeldItems.IsHoldingItemAsWeapon(item.GrabbableObject))
                        {
                            if (i == 0)
                            {
                                return item.GrabbableObject;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        return item.GrabbableObject;
                    }
                    return null;
                default:
                    return null;
            }
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

        public void DropAllItems(EnumOptionsGetItems dropOptions, bool waitBetweenItems = true)
        {
            if (waitBetweenItems)
            {
                if (!dropAllObjectsCoroutineRunning)
                {
                    if (dropAllObjectsCoroutine != null)
                    {
                        StopCoroutine(dropAllObjectsCoroutine);
                    }
                    dropAllObjectsCoroutine = StartCoroutine(DropAllItemsCoroutine(dropOptions));
                }
            }
            else
            {
                while (!this.AreHandsFree())
                {
                    GrabbableObject? itemToDrop = ChooseLastPickedUpItem(dropOptions);
                    if (itemToDrop == null)
                    {
                        break;
                    }

                    DropItem(itemToDrop);
                }
            }
        }

        private IEnumerator DropAllItemsCoroutine(EnumOptionsGetItems dropOptions)
        {
            dropAllObjectsCoroutineRunning = true;
            while (!this.AreHandsFree())
            {
                GrabbableObject? itemToDrop = ChooseLastPickedUpItem(dropOptions);
                if (itemToDrop == null)
                {
                    break;
                }

                DropItem(itemToDrop);
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
            HeldItems.DropItem(grabbableObject);

            InternManager.Instance.AddToDictJustDroppedItems(grabbableObject);

            NpcController.Npc.isHoldingObject = HeldItems.IsHoldingAnItem();
            NpcController.Npc.currentlyHeldObjectServer = HeldItems.GetCurrentlyHeldItem(ignoreWeapon: PluginRuntimeProvider.Context.Config.CanUseWeapons);
            NpcController.Npc.twoHanded = IsHoldingTwoHandedItem();
            NpcController.Npc.twoHandedAnimation = ShouldUseTwoHandedHoldAnim();
            NpcController.GrabbedObjectValidated = false;

            // Animations
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_GRAB, NpcController.Npc.isHoldingObject);
            SetSpecialGrabAnimationBool(NpcController.Npc.twoHandedAnimation, "HoldLung");
            NpcController.Npc.playerBodyAnimator.SetBool(Const.PLAYER_ANIMATION_BOOL_CANCELHOLDING, !npcController.Npc.isHoldingObject);
            NpcController.Npc.playerBodyAnimator.SetTrigger(Const.PLAYER_ANIMATION_TRIGGER_THROW);

            // New weight
            float weightToLose = grabbableObject.itemProperties.weight - 1f < 0f ? 0f : grabbableObject.itemProperties.weight - 1f;
            NpcController.Npc.carryWeight = Mathf.Clamp(NpcController.Npc.carryWeight - weightToLose, 1f, 10f);

            // Battery
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

using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;

namespace LethalInternship.Core.Interns.AI.Items
{
    public class HeldItems
    {
        public List<HeldItem> Items;
        public HeldItem? HeldWeapon;

        public bool KeepWeaponForEmergency = true;// Todo : change !
        public int NoWeaponItemCount
        {
            get
            {
                if (IsHoldingWeapon())
                {
                    return Items.Count - 1;
                }
                else
                {
                    return Items.Count;
                }
            }
        }

        public HeldItems()
        {
            Items = new List<HeldItem>();
        }

        public bool IsHoldingAnItem()
        {
            return NoWeaponItemCount > 0;
        }

        public bool IsHoldingItem(GrabbableObject grabbableObject)
        {
            return Items.Any(x => x.GrabbableObject == grabbableObject);
        }

        public bool IsHoldingItemAsWeapon(GrabbableObject grabbableObject)
        {
            return HeldWeapon != null && HeldWeapon.GrabbableObject == grabbableObject;
        }

        public bool IsHoldingTwoHandedItem()
        {
            return Items.Where(x => x.GrabbableObject != null
                                && x.IsTwoHanded
                                && x != HeldWeapon)
                        .Any();
        }

        public bool IsHoldingTwoHandedAnimationItem()
        {
            return Items.Where(x => x.GrabbableObject != null)
                        .Where(x => x != HeldWeapon)
                        .Where(x => x.GrabbableObject!.itemProperties.twoHandedAnimation
                                    || x.GrabbableObject.name.Contains("ShovelItem")
                                    || x.GrabbableObject.name.Contains("StopSign")
                                    || x.GrabbableObject.name.Contains("YieldSign")
                                    || x.GrabbableObject.name.Contains("PatcherGunItem"))
                        .Any();
        }

        public bool IsHoldingWeapon()
        {
            return HeldWeapon != null;
        }

        public int GetFreeSlots()
        {
            return PluginRuntimeProvider.Context.Config.NbMaxCanCarry - NoWeaponItemCount;
        }

        public GrabbableObject? GetFirstPickedUpItem()
        {
            return Items.FirstOrDefault()?.GrabbableObject;
        }

        public GrabbableObject? GetLastPickedUpItem()
        {
            return Items.LastOrDefault()?.GrabbableObject;
        }

        public GrabbableObject? GetTwoHandItem()
        {
            foreach (var item in Items)
            {
                if (item.GrabbableObject == null)
                {
                    continue;
                }

                if (item == HeldWeapon)
                {
                    continue;
                }

                if (item.IsTwoHanded)
                {
                    return item.GrabbableObject;
                }
            }

            return null;
        }

        public GrabbableObject? GetHeldWeapon()
        {
            return HeldWeapon?.GrabbableObject;
        }

        public void HoldItem(GrabbableObject grabbableObject)
        {
            HeldItem newItem = new HeldItem(grabbableObject);
            Items.Add(newItem);
            foreach (var item in Items)
            {
                PluginLoggerHook.LogDebug?.Invoke($"item: {item}");
            }

            if (HeldWeapon == null
                && KeepWeaponForEmergency)
            {
                HeldWeapon = newItem;
            }
        }

        public void DropItem(GrabbableObject grabbableObject)
        {
            Items.RemoveAll(x => x.GrabbableObject == grabbableObject);
            foreach (var item in Items)
            {
                PluginLoggerHook.LogDebug?.Invoke($"item: {item}");
            }

            if (HeldWeapon != null
                && HeldWeapon.GrabbableObject == grabbableObject)
            {
                HeldWeapon = null;
            }
        }

        public void ShowHideAllItemsMeshes(bool show)
        {
            foreach (var item in Items)
            {
                if (item.GrabbableObject == null)
                {
                    continue;
                }

                item.GrabbableObject.EnableItemMeshes(enable: show);
            }
        }
    }
}

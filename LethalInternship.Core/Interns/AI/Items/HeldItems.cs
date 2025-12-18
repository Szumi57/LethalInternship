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

        public bool KeepWeaponForEmergency => PluginRuntimeProvider.Context.Config.CanUseWeapons;
        public int ItemCount
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
            return ItemCount > 0;
        }

        public bool IsHoldingItem(GrabbableObject grabbableObject)
        {
            return Items.Any(x => x.GrabbableObject == grabbableObject);
        }

        public bool IsHoldingItemAsWeapon(GrabbableObject? grabbableObject)
        {
            return HeldWeapon != null
                   && grabbableObject != null
                   && HeldWeapon.GrabbableObject == grabbableObject;
        }

        public bool IsHoldingTwoHandedItem()
        {
            return Items.Where(x => x.GrabbableObject != null
                                && x.IsTwoHanded
                                && x != HeldWeapon)
                        .Any();
        }

        public bool IsHoldingTwoHandedAnimationItem(bool ignoreHeldWeapon)
        {
            var res = Items.Where(x => x.GrabbableObject != null);
            if (ignoreHeldWeapon)
            {
                res = res.Where(x => x != HeldWeapon);
            }

            return res.Where(x => x.GrabbableObject!.itemProperties.twoHandedAnimation
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
            return PluginRuntimeProvider.Context.Config.NbMaxCanCarry - ItemCount;
        }

        public GrabbableObject? GetFirstPickedUpGrabbableObject()
        {
            for (int i = 0; i < ItemCount; i++)
            {
                var item = Items[i];
                if (KeepWeaponForEmergency
                    && IsHoldingItemAsWeapon(item.GrabbableObject))
                {
                    continue;
                }

                return item.GrabbableObject;
            }

            return null;
        }

        public GrabbableObject? GetLastPickedUpGrabbableObject()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                var item = Items[i];
                if (KeepWeaponForEmergency
                    && IsHoldingItemAsWeapon(item.GrabbableObject))
                {
                    continue;
                }

                return item.GrabbableObject;
            }

            return null;
        }

        public HeldItem? GetLastPickedUpHeldItem()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                var item = Items[i];
                if (KeepWeaponForEmergency
                    && IsHoldingItemAsWeapon(item.GrabbableObject))
                {
                    continue;
                }

                return item;
            }

            return null;
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

        public HeldItem? GetHeldWeaponAsHeldItem()
        {
            return HeldWeapon;
        }

        public void HoldItem(GrabbableObject grabbableObject)
        {
            HeldItem newItem = new HeldItem(grabbableObject);
            Items.Add(newItem);
            foreach (var item in Items)
            {
                PluginLoggerHook.LogDebug?.Invoke($"item: {item}");
            }

            if (newItem.IsWeapon
                && HeldWeapon == null
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

        public void ShowHideAllItemsMeshes(bool show, bool includeHeldWeapon = true)
        {
            foreach (var item in Items)
            {
                if (item.GrabbableObject == null)
                {
                    continue;
                }

                if (item == HeldWeapon && !includeHeldWeapon)
                {
                    continue;
                }

                item.GrabbableObject.EnableItemMeshes(enable: show);
            }
        }
    }
}

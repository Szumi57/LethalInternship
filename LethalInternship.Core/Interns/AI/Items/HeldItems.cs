using LethalInternship.SharedAbstractions.Hooks.PluginLoggerHooks;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LethalInternship.Core.Interns.AI.Items
{
    public class HeldItems
    {
        public List<HeldItem> Items;

        public HeldItems()
        {
            Items = new List<HeldItem>();
        }

        public bool IsHoldingAnItem()
        {
            return Items.Count > 0;
        }

        public bool IsHoldingItem(GrabbableObject grabbableObject)
        {
            return Items.Any(x => x.GrabbableObject == grabbableObject);
        }

        public bool IsHoldingTwoHandedItem()
        {
            return Items.Where(x => x.GrabbableObject != null
                                && x.IsTwoHanded)
                        .Any();
        }

        public bool IsHoldingTwoHandedItemWithAnimation()
        {
            return Items.Where(x => x.GrabbableObject != null
                                && x.GrabbableObject.itemProperties.twoHandedAnimation)
                        .Any();
        }

        public int GetFreeSlots()
        {
            return PluginRuntimeProvider.Context.Config.NbMaxCanCarry - Items.Count;
        }

        public GrabbableObject? GetFirstPickedUpItem()
        {
            return Items.FirstOrDefault().GrabbableObject;
        }

        public GrabbableObject? GetLastPickedUpItem()
        {
            return Items.LastOrDefault().GrabbableObject;
        }

        public GrabbableObject? GetTwoHandItem()
        {
            foreach (var item in Items)
            {
                if (item.GrabbableObject == null)
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

        public void HoldItem(GrabbableObject grabbableObject)
        {
            Items.Add(new HeldItem(grabbableObject));
            foreach (var item in Items)
            {
                PluginLoggerHook.LogDebug?.Invoke($"item: {item}");
            }
        }

        public void DropItem(GrabbableObject grabbableObject)
        {
            Items.RemoveAll(x => x.GrabbableObject == grabbableObject);
            foreach (var item in Items)
            {
                PluginLoggerHook.LogDebug?.Invoke($"item: {item}");
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

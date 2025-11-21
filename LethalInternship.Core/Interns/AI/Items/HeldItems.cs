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

        public bool IsNewItemAllowed()
        {
            return true;
        }

        public bool IsHoldingAnItem()
        {
            return Items.Count > 0;
        }

        public bool IsHoldingItem(GrabbableObject grabbableObject)
        {
            return Items.Any(x => x.GrabbableObject == grabbableObject);
        }

        public GrabbableObject? GetFirstPickedUpItem()
        {
            return Items.FirstOrDefault().GrabbableObject;
        }

        public GrabbableObject? GetLastPickedUpItem()
        {
            return Items.LastOrDefault().GrabbableObject;
        }

        public void HoldItem(GrabbableObject grabbableObject)
        {
            Items.Add(new HeldItem(grabbableObject));
        }

        public void DropItem(GrabbableObject grabbableObject)
        {
            Items.RemoveAll(x => x.GrabbableObject == grabbableObject);
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

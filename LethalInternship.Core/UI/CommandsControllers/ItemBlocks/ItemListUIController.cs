using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.UI.CommandsControllers.ItemBlocks;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalInternship.Core.UI.ItemBlocks
{
    public class ItemListUIController : MonoBehaviour, IRefreshableUI
    {
        public Transform Content = null!;
        public ItemBlockUI PrefabItemBlockUI = null!;
        public CategoryBlockUI PrefabCategoryBlockUI = null!;

        private BlocksUIPool<CategoryBlockUI> categoryPool = null!;

        private Dictionary<GrabbableObject, ItemBlockUI> blocksByGrabbableObject = new Dictionary<GrabbableObject, ItemBlockUI>();
        private Dictionary<EnumCategoryTypeUI, CategoryBlockUI> categoryMap = new Dictionary<EnumCategoryTypeUI, CategoryBlockUI>();

        private int weaponScrapValue = 0;
        private int itemsScrapValue = 0;

        IInternAI currentInternAI = null!;

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            InitUIPools();
            if (GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            IInternIdentity? identity = IdentitySelectionService.Instance.GetCurrent();
            if (identity == null
                || !identity.Alive
                || identity.InternAI == null)
            {
                return;
            }
            this.currentInternAI = identity.InternAI;

            SyncList(currentInternAI.GetHeldGrabbableObjects());

            // Event
            currentInternAI.OnHeldItemsChanged -= UpdateItems;
            currentInternAI.OnHeldItemsChanged += UpdateItems;
        }

        private void InitUIPools()
        {
            if (categoryPool == null)
            {
                categoryPool = new BlocksUIPool<CategoryBlockUI>(PrefabCategoryBlockUI, Content, Enum.GetNames(typeof(EnumCategoryTypeUI)).Length);
            }
        }

        private void UpdateItems(IInternAI intern)
        {
            SyncList(currentInternAI.GetHeldGrabbableObjects());
        }

        public void SyncList(IEnumerable<GrabbableObject> items)
        {
            // Reset
            weaponScrapValue = 0;
            itemsScrapValue = 0;

            // Init categories
            var grouped = new Dictionary<EnumCategoryTypeUI, List<GrabbableObject>>
            {
                { EnumCategoryTypeUI.HeldWeapon, new List<GrabbableObject>() },
                { EnumCategoryTypeUI.HeldItem, new List<GrabbableObject>() },
            };

            foreach (var itemGrabbableObject in items)
                grouped[GetCategory(itemGrabbableObject)].Add(itemGrabbableObject);

            DisableMissingItems(items);

            int siblingIndex = 0;

            siblingIndex = SyncCategory(
                EnumCategoryTypeUI.HeldWeapon,
                grouped[EnumCategoryTypeUI.HeldWeapon],
                weaponScrapValue,
                siblingIndex
            );

            siblingIndex = SyncCategory(
                EnumCategoryTypeUI.HeldItem,
                grouped[EnumCategoryTypeUI.HeldItem],
                itemsScrapValue,
                siblingIndex
            );
        }

        private EnumCategoryTypeUI GetCategory(GrabbableObject item)
        {
            if (currentInternAI.GetHeldWeapon() == item)
            {
                weaponScrapValue += item.scrapValue;
                return EnumCategoryTypeUI.HeldWeapon;
            }

            itemsScrapValue += item.scrapValue;
            return EnumCategoryTypeUI.HeldItem;
        }

        private int SyncCategory(EnumCategoryTypeUI type,
                                 List<GrabbableObject> itemsGrabbableObjects,
                                 int totalValue,
                                 int startIndex)
        {
            //if (itemIDs.Count == 0)
            //{
            //    RemoveCategory(type);
            //    return startIndex;
            //}

            var category = GetOrCreateCategory(type, itemsGrabbableObjects.Count, totalValue);
            category.transform.SetSiblingIndex(startIndex++);

            foreach (var itemGrabbableObject in itemsGrabbableObjects)
            {
                if (!blocksByGrabbableObject.TryGetValue(itemGrabbableObject, out ItemBlockUI block))
                {
                    // Get intern block
                    block = Object.Instantiate(PrefabItemBlockUI, Content);
                    block.Setup(itemGrabbableObject);

                    blocksByGrabbableObject[itemGrabbableObject] = block;
                }

                block.gameObject.SetActive(true);
                block.transform.SetSiblingIndex(startIndex++);
            }

            return startIndex;
        }

        private void DisableMissingItems(IEnumerable<GrabbableObject> newItems)
        {
            var set = new HashSet<GrabbableObject>(newItems);
            var toDisable = new List<GrabbableObject>();

            foreach (var item in blocksByGrabbableObject.Keys)
                if (!set.Contains(item))
                    toDisable.Add(item);

            foreach (var item in toDisable)
            {
                blocksByGrabbableObject[item].gameObject.SetActive(false);
            }
        }

        private void RemoveCategory(EnumCategoryTypeUI type)
        {
            if (!categoryMap.TryGetValue(type, out var cat))
                return;

            categoryPool.Release(cat);
            categoryMap.Remove(type);
        }

        private CategoryBlockUI GetOrCreateCategory(EnumCategoryTypeUI type, int count, int totalValue)
        {
            if (!categoryMap.TryGetValue(type, out var cat))
            {
                cat = categoryPool.Get();
                cat.SetCategory(type);
                categoryMap[type] = cat;
            }

            cat.UpdateTitleText(string.Format($"{UIConst.CATEGORIES_STRING[(int)type]}", count, $"{totalValue}"));
            return cat;
        }
    }
}

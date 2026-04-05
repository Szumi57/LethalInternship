using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.UI.CommandsControllers.ItemBlocks;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.UI.ItemBlocks
{
    public class ItemListUIController : MonoBehaviour
    {
        public Transform Content = null!;
        public ItemBlockUI PrefabItemBlockUI = null!;
        public CategoryBlockUI PrefabCategoryBlockUI = null!;

        private BlocksUIPool<ItemBlockUI> itemsPool = null!;
        private BlocksUIPool<CategoryBlockUI> categoryPool = null!;

        private Dictionary<int, ItemBlockUI> blocksByUID = new Dictionary<int, ItemBlockUI>();
        private Dictionary<string, List<ItemBlockUI>> blocksByItemName = new Dictionary<string, List<ItemBlockUI>>();

        private Dictionary<EnumCategoryTypeUI, CategoryBlockUI> categoryMap = new Dictionary<EnumCategoryTypeUI, CategoryBlockUI>();

        private int weaponScrapValue = 0;
        private int itemsScrapValue = 0;

        IInternAI currentInternAI = null!;

        void Start()
        {
            InitUIPools();
        }

        void OnEnable()
        {
            InitUIPools();
            if (GameNetworkManager.Instance == null
                || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            IInternIdentity? identity = IdentitySelectionService.Instance.SelectedInterns.FirstOrDefault();
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

        void OnDisable()
        {
            ReleaseUnusedItemBlocks();
        }

        private void InitUIPools()
        {
            if (itemsPool == null)
            {
                itemsPool = new BlocksUIPool<ItemBlockUI>(PrefabItemBlockUI, Content, PluginRuntimeProvider.Context.Config.NbMaxCanCarry);
            }
            if (categoryPool == null)
            {
                categoryPool = new BlocksUIPool<CategoryBlockUI>(PrefabCategoryBlockUI, Content, Enum.GetNames(typeof(EnumCategoryTypeUI)).Length);
            }
        }

        private void UpdateItems(IInternAI intern)
        {
            SyncList(currentInternAI.GetHeldGrabbableObjects());
        }

        public void SyncList(List<GrabbableObject> items)
        {
            // Reset
            weaponScrapValue = 0;
            itemsScrapValue = 0;

            // Init categories
            var grouped = new Dictionary<EnumCategoryTypeUI, List<ItemUIInfos>>
            {
                { EnumCategoryTypeUI.HeldWeapon, new List<ItemUIInfos>() },
                { EnumCategoryTypeUI.HeldItem, new List<ItemUIInfos>() },
            };

            // Mark all block to unused
            foreach (var block in blocksByUID.Values)
                block.MarkUnused();

            foreach (var item in items)
                grouped[GetCategory(item)].Add(new ItemUIInfos() { ItemName = item.itemProperties.itemName, ItemValue = item.scrapValue });

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
                                 List<ItemUIInfos> itemsInfos,
                                 int totalValue,
                                 int startIndex)
        {
            //if (itemIDs.Count == 0)
            //{
            //    RemoveCategory(type);
            //    return startIndex;
            //}

            var category = GetOrCreateCategory(type, itemsInfos.Count, totalValue);
            category.transform.SetSiblingIndex(startIndex++);

            foreach (var itemInfos in itemsInfos)
            {
                ItemBlockUI block = GetReusableBlock(itemInfos);
                block.MarkUsed();
                block.gameObject.SetActive(true);
                Debug.Log($"UpdateInfos for {itemInfos.ItemName} in block {block.ItemName}");
                Debug.Log($"-----------");
                block.UpdateInfos(itemInfos);

                block.transform.SetSiblingIndex(startIndex++);
            }

            return startIndex;
        }

        private ItemBlockUI GetReusableBlock(ItemUIInfos itemInfos)
        {
            Debug.Log($"blocksByItemName looking for {itemInfos.ItemName}");
            foreach (var (key, value) in blocksByItemName)
            {
                foreach (var blockItem in value)
                {
                    if (blockItem != null)
                        Debug.Log($"blocksByItemName {key} -> {blockItem.ItemName} isUsed:{blockItem.IsUsed}");
                }
            }

            if (blocksByItemName.TryGetValue(itemInfos.ItemName, out var list))
            {
                var reusable = list.FirstOrDefault(b => !b.IsUsed);
                if (reusable != null)
                    return reusable;
            }

            Debug.Log($"new block for {itemInfos.ItemName}");
            // not found -> new block
            var block = itemsPool.Get();
            block.AssignRuntimeUID();
            block.Setup(itemInfos);

            blocksByUID[block.RuntimeUID] = block;

            if (!blocksByItemName.TryGetValue(itemInfos.ItemName, out list))
                blocksByItemName[itemInfos.ItemName] = list = new List<ItemBlockUI>();

            list.Add(block);
            return block;
        }


        private void ReleaseUnusedItemBlocks()
        {
            Debug.Log($"ReleaseUnusedItemBlocks ----------");
            foreach (var (key, value) in blocksByUID)
            {
                if (value != null)
                    Debug.Log($"blocksByUID {key} -> {value.ItemName} isUsed:{value.IsUsed}");
            }

            var toRelease = blocksByUID
                            //.Where(kv => !kv.Value.IsUsed)
                            .Select(kv => kv.Key)
                            .ToList();

            foreach (var uid in toRelease)
            {
                var block = blocksByUID[uid];
                block.MarkUnused();
                itemsPool.Release(block);
                //blocksByUID.Remove(uid);

                Debug.Log($"ReleaseUnusedItemBlocks block {block.ItemName} used:{block.IsUsed}");
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

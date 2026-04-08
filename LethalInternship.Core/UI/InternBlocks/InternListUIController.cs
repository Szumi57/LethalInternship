using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.UI.Others;
using LethalInternship.SharedAbstractions.Constants;
using LethalInternship.SharedAbstractions.Enums;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.InternBlocks
{
    public class InternListUIController : MonoBehaviour
    {
        public Transform Content = null!;
        public InternBlockUI PrefabInternBlockUI = null!;
        public CategoryBlockUI PrefabCategoryBlockUI = null!;

        private BlocksUIPool<InternBlockUI> identitiesPool = null!;
        private BlocksUIPool<CategoryBlockUI> categoryPool = null!;

        private Dictionary<IInternIdentity, InternBlockUI> identityMap = new Dictionary<IInternIdentity, InternBlockUI>();
        private Dictionary<EnumCategoryTypeUI, CategoryBlockUI> categoryMap = new Dictionary<EnumCategoryTypeUI, CategoryBlockUI>();

        void Start()
        {
            InitUIPools();
        }

        void OnEnable()
        {
            InitUIPools();
            if (GameNetworkManager.Instance != null
                && GameNetworkManager.Instance.localPlayerController != null)
            {
                SyncList(IdentitySelectionService.Instance.GetSelected());
            }
        }

        private void InitUIPools()
        {
            if (identitiesPool == null)
            {
                identitiesPool = new BlocksUIPool<InternBlockUI>(PrefabInternBlockUI, Content, PluginRuntimeProvider.Context.Config.MaxInternsAvailable);
            }
            if (categoryPool == null)
            {
                categoryPool = new BlocksUIPool<CategoryBlockUI>(PrefabCategoryBlockUI, Content, Enum.GetNames(typeof(EnumCategoryTypeUI)).Length);
            }
        }

        public void SyncList(IEnumerable<IInternIdentity> identities)
        {
            var grouped = new Dictionary<EnumCategoryTypeUI, List<IInternIdentity>>
            {
                { EnumCategoryTypeUI.InternClose, new List<IInternIdentity>() },
                { EnumCategoryTypeUI.InternTooFar, new List<IInternIdentity>() },
                { EnumCategoryTypeUI.InternDead, new List<IInternIdentity>() }
            };

            foreach (var identity in identities)
                grouped[GetCategory(identity)].Add(identity);

            RemoveMissingInterns(identities);

            int siblingIndex = 0;

            siblingIndex = SyncCategory(
                EnumCategoryTypeUI.InternClose,
                grouped[EnumCategoryTypeUI.InternClose],
                siblingIndex
            );

            siblingIndex = SyncCategory(
                EnumCategoryTypeUI.InternTooFar,
                grouped[EnumCategoryTypeUI.InternTooFar],
                siblingIndex
            );

            siblingIndex = SyncCategory(
                EnumCategoryTypeUI.InternDead,
                grouped[EnumCategoryTypeUI.InternDead],
                siblingIndex
            );
        }

        private EnumCategoryTypeUI GetCategory(IInternIdentity identity)
        {
            if (!identity.Alive)
                return EnumCategoryTypeUI.InternDead;

            IInternAI? intern = identity.InternAI;
            if (intern == null)
                return EnumCategoryTypeUI.InternTooFar;

            if (intern.NpcController.GetSqrDistanceWithLocalPlayer() < UIConst.DISTANCE_UI_PROXIMITY * UIConst.DISTANCE_UI_PROXIMITY)
                return EnumCategoryTypeUI.InternClose;

            return EnumCategoryTypeUI.InternTooFar;
        }

        private int SyncCategory(EnumCategoryTypeUI type,
                                 List<IInternIdentity> identities,
                                 int startIndex)
        {
            //if (interns.Count == 0)
            //{
            //    RemoveCategory(type);
            //    return startIndex;
            //}

            var category = GetOrCreateCategory(type, identities.Count);
            category.transform.SetSiblingIndex(startIndex++);

            foreach (var identity in identities)
            {
                if (!identityMap.TryGetValue(identity, out InternBlockUI block))
                {
                    // Get intern block
                    block = identitiesPool.Get();
                    block.Setup(identity);

                    identityMap[identity] = block;

                    // Link events
                    IInternAI? intern = identity.InternAI;
                    if (intern != null)
                    {
                        intern.OnHeldItemsChanged += UpdateItemsCount;
                        intern.OnInternDead += UpdateInternDead;
                    }
                }

                block.transform.SetSiblingIndex(startIndex++);
            }

            return startIndex;
        }

        private void RemoveMissingInterns(IEnumerable<IInternIdentity> newIdentities)
        {
            var set = new HashSet<IInternIdentity>(newIdentities);
            var toRemove = new List<IInternIdentity>();

            foreach (var intern in identityMap.Keys)
                if (!set.Contains(intern))
                    toRemove.Add(intern);

            foreach (var intern in toRemove)
            {
                identitiesPool.Release(identityMap[intern]);
                identityMap.Remove(intern);
            }
        }

        private void RemoveCategory(EnumCategoryTypeUI type)
        {
            if (!categoryMap.TryGetValue(type, out var cat))
                return;

            categoryPool.Release(cat);
            categoryMap.Remove(type);
        }

        private CategoryBlockUI GetOrCreateCategory(EnumCategoryTypeUI type, int internCount)
        {
            if (!categoryMap.TryGetValue(type, out var cat))
            {
                cat = categoryPool.Get();
                cat.SetCategory(type);
                categoryMap[type] = cat;
            }

            cat.UpdateTitleText(string.Format($"{UIConst.CATEGORIES_STRING[(int)type]}", internCount));
            return cat;
        }

        #region Events

        void UpdateItemsCount(IInternAI intern)
        {
            if (identityMap.TryGetValue(intern.InternIdentity, out var block))
                block.UpdateItemCount();
        }

        void UpdateInternDead(IInternAI intern)
        {
            if (identityMap.TryGetValue(intern.InternIdentity, out var block))
                block.UpdateDeadInternState();
        }

        public void MouseOver()
        {
            UIVisibilityController.Instance.SetOnlyListInternsAndSuitCommandsVisible();
        }

        public void MouseLeave()
        {
            UIVisibilityController.Instance.SetAllVisible();
        }

        #endregion
    }
}

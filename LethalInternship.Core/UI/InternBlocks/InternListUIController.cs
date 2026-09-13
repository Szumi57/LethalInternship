using LethalInternship.Core.CommandsSystem;
using LethalInternship.Core.Managers;
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
    public class InternListUIController : MonoBehaviour, IVisibilityUI
    {
        public GameObject Go { get; private set; } = null!;
        public EnumUIGroups GroupUI = EnumUIGroups.InternsList;
        EnumUIGroups IGroupUI.GroupUI => this.GroupUI;

        public Transform Content = null!;
        public InternBlockUI PrefabInternBlockUI = null!;
        public CategoryBlockUI PrefabCategoryBlockUI = null!;

        private BlocksUIPool<InternBlockUI> identitiesPool = null!;
        private BlocksUIPool<CategoryBlockUI> categoryPool = null!;

        private Dictionary<IInternIdentity, InternBlockUI> identityMap = new Dictionary<IInternIdentity, InternBlockUI>();
        private Dictionary<EnumCategoryTypeUI, CategoryBlockUI> categoryMap = new Dictionary<EnumCategoryTypeUI, CategoryBlockUI>();

        void Awake()
        {
            Go = null!; // we don't want the whole panel to be disabled
        }

        void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            InitUIPools();
            if (GameNetworkManager.Instance != null
                && GameNetworkManager.Instance.localPlayerController != null)
            {
                SyncList(IdentitySelectionService.Instance.GetSelected());
            }
        }

        public void SetInteractable(bool interactable, string tooltipMessageNotInteractable = null!)
        {
            foreach (var (identity, block) in identityMap)
            {
                if (interactable)
                {
                    if (GetCategory(identity) != EnumCategoryTypeUI.InternClose)
                    {
                        interactable = false;
                    }
                }
                block.SetInteractable(interactable, tooltipMessageNotInteractable);
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
                { EnumCategoryTypeUI.InternNotOwned, new List<IInternIdentity>() },
                { EnumCategoryTypeUI.InternDead, new List<IInternIdentity>() }
            };

            foreach (var identity in identities)
            {
                grouped[GetCategory(identity)].Add(identity);
            }

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
                EnumCategoryTypeUI.InternNotOwned,
                grouped[EnumCategoryTypeUI.InternNotOwned],
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
                return EnumCategoryTypeUI.InternNotOwned;

            if (StartOfRound.Instance != null
                && StartOfRound.Instance.localPlayerController != null
                && intern.OwnerClientId != StartOfRound.Instance.localPlayerController.actualClientId)
            {
                return EnumCategoryTypeUI.InternNotOwned;
            }
            if (intern.IsSpawningAnimationRunning())
            {
                return EnumCategoryTypeUI.InternNotOwned;
            }

            if (intern.NpcController.GetSqrDistanceWithLocalPlayer() < InternManager.Instance.GetMaxDistanceCommand())
            {
                return EnumCategoryTypeUI.InternClose;
            }

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
                        intern.OnHeldItemsChanged -= RefreshBlock;
                        intern.OnInternDead -= RefreshList;
                        intern.InternIdentity.OnAutoDefenseChanged -= RefreshBlock;
                        intern.InternIdentity.OnCommandChanged -= RefreshBlock;
                        intern.OnOwnerChanged -= RefreshList;

                        intern.OnHeldItemsChanged += RefreshBlock;
                        intern.OnInternDead += RefreshList;
                        intern.InternIdentity.OnAutoDefenseChanged += RefreshBlock;
                        intern.InternIdentity.OnCommandChanged += RefreshBlock;
                        intern.OnOwnerChanged += RefreshList;
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

        private void RefreshList(IInternAI intern)
        {
            Init();
        }

        private void RefreshBlock(IInternAI intern)
        {
            RefreshBlock(intern.InternIdentity);
        }

        private void RefreshBlock(IInternIdentity identity)
        {
            if (identityMap.TryGetValue(identity, out var block))
                ((IRefreshableUI)block).Refresh();
        }

        public void MouseOver()
        {
            UIManager.Instance.CommandsAllController.SetOnlyListInternsAndSuitCommandsVisible();
        }

        public void MouseLeave()
        {
            UIManager.Instance.CommandsAllController.SetAllVisible();
        }

        #endregion
    }
}

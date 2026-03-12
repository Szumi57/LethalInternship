using LethalInternship.Core.Managers;
using LethalInternship.SharedAbstractions.Interns;
using LethalInternship.SharedAbstractions.PluginRuntimeProvider;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers.InternBlocks
{
    public class InternListUIController : MonoBehaviour
    {
        public Transform Content = null!;
        public InternBlockUI Prefab = null!;

        private BlocksUIPool<InternBlockUI> blocksUIPool = null!;
        private Dictionary<IInternAI, InternBlockUI> map = new Dictionary<IInternAI, InternBlockUI>();

        void Start()
        {
            InitBlocksUIPool();
        }

        void OnEnable()
        {
            InitBlocksUIPool();
            IInternAI[] internsOwned = InternManager.Instance.GetAliveAndSpawnInternsAIOwnedByLocal();
            SyncList(internsOwned.ToList());
        }

        private void InitBlocksUIPool()
        {
            if (blocksUIPool == null)
            {
                blocksUIPool = new BlocksUIPool<InternBlockUI>(Prefab, Content, PluginRuntimeProvider.Context.Config.MaxInternsAvailable);
            }
        }

        public void AddIntern(IInternAI intern)
        {
            var block = blocksUIPool.Get();
            block.Setup(intern);

            map[intern] = block;

            intern.OnHeldItemsChanged += UpdateItemsCount;
        }

        void UpdateItemsCount(IInternAI intern)
        {
            if (map.TryGetValue(intern, out var block))
                block.UpdateItemCount();
        }

        public void RemoveIntern(IInternAI intern)
        {
            if (map.TryGetValue(intern, out var block))
            {
                blocksUIPool.Release(block);
                map.Remove(intern);
            }
        }

        public void SyncList(List<IInternAI> newInterns)
        {
            HashSet<IInternAI> newSet = new HashSet<IInternAI>(newInterns);

            // Remove those not in list
            List<IInternAI> toRemove = new List<IInternAI>();

            foreach (var intern in map.Keys)
            {
                if (!newSet.Contains(intern))
                    toRemove.Add(intern);
            }

            foreach (var intern in toRemove)
                RemoveIntern(intern);

            // Add missing ones
            foreach (var intern in newInterns)
            {
                if (!map.ContainsKey(intern))
                    AddIntern(intern);
            }
        }
    }
}

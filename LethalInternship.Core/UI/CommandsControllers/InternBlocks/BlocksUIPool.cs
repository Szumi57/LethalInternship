using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.UI.CommandsControllers.InternBlocks
{
    public class BlocksUIPool<T> where T : Component
    {
        T prefab;
        Transform parent;

        Queue<T> pool = new Queue<T>();

        public BlocksUIPool(T prefab, Transform parent, int startSize)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < startSize; i++)
            {
                var obj = Object.Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            if (pool.Count == 0)
                Expand();

            var obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

        void Expand()
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}

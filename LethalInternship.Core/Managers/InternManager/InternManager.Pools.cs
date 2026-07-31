using LethalInternship.SharedAbstractions.Managers;
using System;
using System.Collections.Generic;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region Object pools

        public IPoolManager Pools = new PoolManager();

        public class PoolManager : IPoolManager
        {
            private readonly Dictionary<Type, object> pools = new Dictionary<Type, object>();

            public T Get<T>() where T : class, new()
            {
                if (!pools.TryGetValue(typeof(T), out var poolObj))
                {
                    poolObj = new ObjectPool<T>();
                    pools.Add(typeof(T), poolObj);
                }

                return ((ObjectPool<T>)poolObj).Get();
            }

            public void Return<T>(T obj) where T : class, new()
            {
                ((ObjectPool<T>)pools[typeof(T)]).Return(obj);
            }
        }

        public class ObjectPool<T> where T : class, new()
        {
            private readonly Stack<T> pool = new Stack<T>();

            public T Get()
            {
                return pool.Count > 0 ? pool.Pop() : new T();
            }

            public void Return(T obj)
            {
                pool.Push(obj);
            }
        }

        #endregion
    }
}

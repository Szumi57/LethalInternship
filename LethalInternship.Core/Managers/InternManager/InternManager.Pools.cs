using LethalInternship.SharedAbstractions.Pools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalInternship.Core.Managers
{
    public partial class InternManager
    {
        #region Object pools

        public IPoolManager Pools = new PoolManager();

        public class PoolManager : IPoolManager
        {
            private readonly Dictionary<Type, IObjectPool> pools = new Dictionary<Type, IObjectPool>();

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

            public void LogStats()
            {
                foreach (var pool in pools.Values)
                {
                    Debug.Log(
                        $"{pool.ObjectType.Name} : " +
                        $"Created={pool.Created} " +
                        $"Available={pool.Available} " +
                        $"InUse={pool.Created - pool.Available}");
                }
            }
        }

        public class ObjectPool<T> : IObjectPool where T : class, new()
        {
            private readonly Stack<T> pool = new Stack<T>();

            public int Created { get; private set; }

            public int Available => pool.Count;

            public Type ObjectType => typeof(T);

            public T Get()
            {
                if (pool.Count > 0)
                {
                    return pool.Pop();
                }

                Created++;
                return new T();
            }

            public void Return(T obj)
            {
                pool.Push(obj);
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PolarityProtocol.Pooling
{
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Queue<T> available = new();
        private readonly Func<T> factory;

        public ComponentPool(Func<T> create)
        {
            factory = create;
        }

        public int AvailableCount => available.Count;
        public int TotalCreated { get; private set; }

        public T Get()
        {
            T item;
            if (available.Count > 0)
            {
                item = available.Dequeue();
            }
            else
            {
                item = factory();
                TotalCreated++;
            }

            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
            }

            if (TotalCreated == 0 || item.gameObject.name.EndsWith("(New)"))
            {
                item.gameObject.name = item.gameObject.name.Replace("(New)", string.Empty);
            }

            return item;
        }

        public void Release(T item)
        {
            item.gameObject.SetActive(false);
            available.Enqueue(item);
        }
    }
}

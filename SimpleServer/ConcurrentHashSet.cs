using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SimpleServer
{
    internal class ConcurrentHashSet<T>: IEnumerable<T>
    {
        private ConcurrentDictionary<T, byte> store;

        public ConcurrentHashSet()
        {
            store = new ConcurrentDictionary<T, byte>();
        }

        public bool TryAdd(T elem) => store.TryAdd(elem, 0);

        public void Clear()
        {
            store.Clear();
        }

        public bool Contains(T elem) => store.ContainsKey(elem);

        public bool TryRemove(T elem) => store.TryRemove(elem, out byte _);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach(var keyValue in store)
            {
                yield return keyValue.Key;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var keyValue in store)
            {
                yield return keyValue.Key;
            }
        }
    }
}

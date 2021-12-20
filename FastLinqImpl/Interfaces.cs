using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace Fast.Collections
{
    public interface IFastEnumerable<TElement, TEnumerator>
    {
        TEnumerator Start { get; }
        bool TryGetNext(ref TEnumerator enumerator, out TElement? value);
    }
    
    public interface IFastOrderedEnumerable<TElement, TEnumerator> : IFastEnumerable<TElement, TEnumerator>
    {
        IFastOrderedEnumerable<TElement, TEnumerator> CreateOrderedEnumerable<TKey> (Func<TElement,TKey> keySelector, Func<TKey?, TKey?, int>? comparer, bool descending);
    }

    public interface IFastAsyncEnumerable<TElement, TEnumerator>
    {
        TEnumerator Start { get; }
        ValueTask<bool> TryGetNextAsync(ref TEnumerator enumerator, out TElement? value);
    }

    public interface IFastCollection<TElement, TEnumerator> : IFastEnumerable<TElement, TEnumerator>
    {
        int Count { get; }
        bool IsReadOnly { get; }

        void Add(TElement element);
        bool Remove(TElement element);
        bool Contains(TElement element);
        void Clear();
        void CopyTo(TElement[] array, int index = 0);
    }

    public interface IFastList<TElement, TEnumerator> : IFastCollection<TElement, TEnumerator>
    {
        public TElement this[int index] { get; set; }
        
        int IndexOf(TElement element);
        void Insert(int index, TElement element);
        void RemoveAt(int index);
    }
}
using System.Diagnostics;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using Fast.Collections.Generic;

#nullable enable

namespace Fast.Linq
{
    using System;
    using Fast.Collections;

    public static partial class SpanEnumerable
    {
        public static IFastEnumerable<TResult, TEnumerator> Select<TSource, TResult, TEnumerator>(
            this IFastEnumerable<TSource, TEnumerator> source, Func<TSource?, TResult?> selector)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            return new SelectSpanEnumerable<TSource, TResult, TEnumerator>(source, selector);
        }

        public static IFastEnumerable<TElement, TEnumerator> Where<TElement, TEnumerator>(
            this IFastEnumerable<TElement, TEnumerator> source, Predicate<TElement?> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            return new WhereSpanEnumerable<TElement, TEnumerator>(source, predicate);
        }

        public static IFastOrderedEnumerable<TSource, int> OrderBy<TSource, TEnumerator, TKey>(
            this IFastEnumerable<TSource, TEnumerator> source,
            Func<TSource, TKey> keySelector)
        {
            var list = FastList<TSource>.FromEnumerable<TSource, TEnumerator>(source);
            return new FastOrderedEnumerable<TSource, TKey>(list, keySelector);
        }
        
        public static TSource? First<TSource, TEnumerator>(
            this IFastEnumerable<TSource, TEnumerator> source)
        {
            var start = source.Start;
            source.TryGetNext(ref start, out var first);
            return  first;
        }
        
        public static TSource? FirstOrDefault<TSource, TEnumerator>(
            this IFastEnumerable<TSource, TEnumerator> source)
        {
            var start = source.Start;
            source.TryGetNext(ref start, out var first);
            return  first;
        }

        public static TSource Aggregate<TSource, TEnumerator>(
            this IFastEnumerable<TSource, TEnumerator> source,
            Func<TSource, TSource, TSource> func)
        {
            var enumerator = source.Start;
            TSource? result = default;
            source.TryGetNext(ref enumerator, out result);
            while (source.TryGetNext(ref enumerator, out var otherValue))
            {
                result = func(result, otherValue);
            }

            return result;
        }
        
        public static TAccumulate Aggregate<TSource, TEnumerator, TAccumulate>(
            this IFastEnumerable<TSource, TEnumerator> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func)
        {
            var result = seed;
            var enumerator = source.Start;
            while (source.TryGetNext(ref enumerator, out var otherValue))
            {
                result = func(result, otherValue);
            }

            return result;
        }

        public static IFastEnumerable<TSource, TEnumerator> DefaultIfEmpty<TSource, TEnumerator>(
            this IFastEnumerable<TSource, TEnumerator> source,
            TSource defaultValue)
        {
            return new DefaultIfEmptyIterator<TSource, TEnumerator>(source, defaultValue);
        }

        private class DefaultIfEmptyIterator<TSource, TEnumerator> : IFastEnumerable<TSource, TEnumerator>
        {
            private readonly IFastEnumerable<TSource, TEnumerator> _source;
            private readonly TSource _default;
            private int _state = 1;

            public DefaultIfEmptyIterator(IFastEnumerable<TSource,TEnumerator> source, TSource defaultValue)
            {
                _source = source;
                _default = defaultValue;
            }

            public TEnumerator Start => _source.Start;

            public bool TryGetNext(ref TEnumerator enumerator, out TSource? value)
            {
                switch (_state)
                {
                    case 1:
                        if (_source.TryGetNext(ref enumerator, out value))
                        {
                            _state = 2;
                        }
                        else
                        {
                            value = _default;
                        }
                        return true;
                    case 2:
                        return _source.TryGetNext(ref enumerator, out value);
                }

                value = default;
                return false;
            }
        }

        private class SelectSpanEnumerable<TSource, TResult, TEnumerator> : IFastEnumerable<TResult, TEnumerator>
        {
            private readonly IFastEnumerable<TSource, TEnumerator> _source;
            private readonly Func<TSource?, TResult?> _selector;

            public SelectSpanEnumerable(
                IFastEnumerable<TSource, TEnumerator> source,
                Func<TSource?, TResult?> selector)
            {
                _source = source;
                _selector = selector;
                Start = source.Start;
            }

            public TEnumerator Start { get; }

            public bool TryGetNext(ref TEnumerator enumerator, out TResult? value)
            {
                if (_source.TryGetNext(ref enumerator, out var element))
                {
                    value = _selector(element);
                    return value is not null;
                }

                value = default;
                return false;
            }
        }

        private class WhereSpanEnumerable<TElement, TEnumerator> : IFastEnumerable<TElement, TEnumerator>
        {
            private readonly IFastEnumerable<TElement, TEnumerator> _source;
            private readonly Predicate<TElement?> _predicate;

            public WhereSpanEnumerable(
                IFastEnumerable<TElement, TEnumerator> source,
                Predicate<TElement?> predicate)
            {
                _source = source;
                _predicate = predicate;
                Start = source.Start;
            }

            public TEnumerator Start { get; }

            public bool TryGetNext(ref TEnumerator enumerator, out TElement? value)
            {
                while (_source.TryGetNext(ref enumerator, out var element))
                {
                    if (!_predicate(element)) continue;
                    value = element;
                    return true;
                }

                value = default;
                return false;
            }
        }

        private class FastOrderedEnumerable<TElement, TKey> : IFastOrderedEnumerable<TElement, int>
        {
            private Lazy<EnumerableSorter<TElement, int, TKey>> _sorter;
            private Lazy<int[]> _map;
            private readonly IFastList<TElement, int> _source;

            public FastOrderedEnumerable(
                IFastList<TElement, int> source,
                Func<TElement, TKey> keySelector,
                Func<TKey?, TKey?, int>? comparer = null,
                bool descending = false)
            {
                comparer = comparer ?? System.Collections.Generic.Comparer<TKey>.Default.Compare;
                _source = source;
                _sorter = new Lazy<EnumerableSorter<TElement, int, TKey>>(() =>
                    new EnumerableSorter<TElement, int, TKey>(keySelector, comparer, descending, null));
                _map = new Lazy<int[]>(() =>
                {
                    var buffer = source;
                    if (buffer.Count <= 0) return Array.Empty<int>();
                    var sorter = _sorter.Value;
                    return sorter.Sort(buffer, buffer.Count);
                });
            }

            public int Start => 0;

            public bool TryGetNext(ref int enumerator, out TElement? value)
            {
                var buffer = _source;
                var map = _map.Value;
                if (map.Length > 0)
                {
                    value = buffer[map[enumerator]];
                    return true;
                }

                value = default;
                return false;
            }

            public IFastOrderedEnumerable<TElement, int> CreateOrderedEnumerable<TKey1>(
                Func<TElement, TKey1> keySelector, Func<TKey1?, TKey1?, int>? comparer, bool @descending)
            {
                throw new NotImplementedException();
            }
        }

        internal abstract class EnumerableSorter<TElement, TEnumerator>
        {
            internal abstract void ComputeKeys(IFastList<TElement, TEnumerator> elements, int count);

            internal abstract int CompareAnyKeys(int index1, int index2);

            private int[] ComputeMap(IFastList<TElement, TEnumerator> elements, int count)
            {
                ComputeKeys(elements, count);
                var map = new int[count];
                for (var i = 0; i < map.Length; i++)
                {
                    map[i] = i;
                }

                return map;
            }

            internal int[] Sort(IFastList<TElement, TEnumerator> elements, int count)
            {
                var map = ComputeMap(elements, count);
                QuickSort(map, 0, count - 1);
                return map;
            }

            internal int[] Sort(IFastList<TElement, TEnumerator> elements, int count, int minIdx, int maxIdx)
            {
                var map = ComputeMap(elements, count);
                PartialQuickSort(map, 0, count - 1, minIdx, maxIdx);
                return map;
            }

            internal TElement ElementAt(IFastList<TElement, TEnumerator> elements, int count, int idx)
            {
                var map = ComputeMap(elements, count);
                return idx == 0 ? elements[Min(map, count)] : elements[QuickSelect(map, count - 1, idx)];
            }

            protected abstract void QuickSort(int[] map, int left, int right);

            // Sorts the k elements between minIdx and maxIdx without sorting all elements
            // Time complexity: O(n + k log k) best and average case. O(n^2) worse case.
            protected abstract void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx);

            // Finds the element that would be at idx if the collection was sorted.
            // Time complexity: O(n) best and average case. O(n^2) worse case.
            protected abstract int QuickSelect(int[] map, int right, int idx);

            protected abstract int Min(int[] map, int count);
        }

        internal sealed class EnumerableSorter<TElement, TEnumerator, TKey> : EnumerableSorter<TElement, TEnumerator>
        {
            private readonly Func<TElement, TKey> _keySelector;
            private readonly Func<TKey?, TKey?, int> _comparer;
            private readonly bool _descending;
            private readonly EnumerableSorter<TElement, TEnumerator>? _next;
            private TKey[]? _keys;

            internal EnumerableSorter(Func<TElement, TKey> keySelector, Func<TKey?, TKey?, int> comparer,
                bool descending, EnumerableSorter<TElement, TEnumerator>? next)
            {
                _keySelector = keySelector;
                _comparer = comparer;
                _descending = descending;
                _next = next;
            }

            internal override void ComputeKeys(IFastList<TElement, TEnumerator> elements, int count)
            {
                _keys = new TKey[count];
                for (var i = 0; i < count; i++)
                {
                    _keys[i] = _keySelector(elements[i]);
                }

                _next?.ComputeKeys(elements, count);
            }

            internal override int CompareAnyKeys(int index1, int index2)
            {
                var c = _comparer(_keys[index1]!, _keys[index2]);
                if (c != 0) return (_descending != (c > 0)) ? 1 : -1;
                if (_next == null)
                {
                    return index1 - index2; // ensure stability of sort
                }

                return _next.CompareAnyKeys(index1, index2);

                // -c will result in a negative value for int.MinValue (-int.MinValue == int.MinValue).
                // Flipping keys earlier is more likely to trigger something strange in a comparer,
                // particularly as it comes to the sort being stable.
            }

            private int CompareKeys(int index1, int index2) => index1 == index2 ? 0 : CompareAnyKeys(index1, index2);

            protected override void QuickSort(int[] keys, int lo, int hi) =>
                new Span<int>(keys, lo, hi - lo + 1).Sort(CompareAnyKeys);

            // Sorts the k elements between minIdx and maxIdx without sorting all elements
            // Time complexity: O(n + k log k) best and average case. O(n^2) worse case.
            protected override void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx)
            {
                do
                {
                    var i = left;
                    var j = right;
                    var x = map[i + ((j - i) >> 1)];
                    do
                    {
                        while (i < map.Length && CompareKeys(x, map[i]) > 0)
                        {
                            i++;
                        }

                        while (j >= 0 && CompareKeys(x, map[j]) < 0)
                        {
                            j--;
                        }

                        if (i > j)
                        {
                            break;
                        }

                        if (i < j)
                        {
                            (map[i], map[j]) = (map[j], map[i]);
                        }

                        i++;
                        j--;
                    } while (i <= j);

                    if (minIdx >= i)
                    {
                        left = i + 1;
                    }
                    else if (maxIdx <= j)
                    {
                        right = j - 1;
                    }

                    if (j - left <= right - i)
                    {
                        if (left < j)
                        {
                            PartialQuickSort(map, left, j, minIdx, maxIdx);
                        }

                        left = i;
                    }
                    else
                    {
                        if (i < right)
                        {
                            PartialQuickSort(map, i, right, minIdx, maxIdx);
                        }

                        right = j;
                    }
                } while (left < right);
            }

            // Finds the element that would be at idx if the collection was sorted.
            // Time complexity: O(n) best and average case. O(n^2) worse case.
            protected override int QuickSelect(int[] map, int right, int idx)
            {
                var left = 0;
                do
                {
                    var i = left;
                    var j = right;
                    var x = map[i + ((j - i) >> 1)];
                    do
                    {
                        while (i < map.Length && CompareKeys(x, map[i]) > 0)
                        {
                            i++;
                        }

                        while (j >= 0 && CompareKeys(x, map[j]) < 0)
                        {
                            j--;
                        }

                        if (i > j)
                        {
                            break;
                        }

                        if (i < j)
                        {
                            (map[i], map[j]) = (map[j], map[i]);
                        }

                        i++;
                        j--;
                    } while (i <= j);

                    if (i <= idx)
                    {
                        left = i + 1;
                    }
                    else
                    {
                        right = j - 1;
                    }

                    if (j - left <= right - i)
                    {
                        if (left < j)
                        {
                            right = j;
                        }

                        left = i;
                    }
                    else
                    {
                        if (i < right)
                        {
                            left = i;
                        }

                        right = j;
                    }
                } while (left < right);

                return map[idx];
            }

            protected override int Min(int[] map, int count)
            {
                var index = 0;
                for (var i = 1; i < count; i++)
                {
                    if (CompareKeys(map[i], map[index]) < 0)
                    {
                        index = i;
                    }
                }

                return map[index];
            }
        }
    }
}
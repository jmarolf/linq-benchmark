namespace Fast.Collections.Generic
{
    using System;
    
    public static class  FastEnumerable
    {
        public static IFastEnumerable<int, int> Range(int start, int count)
        {
            long num = (long) start + (long) count - 1L;
            if (count < 0 || num > (long) int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));
            return count == 0 ? FastEnumerable.Empty<int>() : new FastEnumerable.RangeIterator(start, count);
        }

        public static IFastEnumerable<T, int> Empty<T>()
        {
            return new EmptyEnumerable<T>();
        }
        
        private class EmptyEnumerable<T> : IFastEnumerable<T, int>
        {
            public int Start { get; } = 0;
        
            public bool TryGetNext(ref int index, out T value)
            {
                value = default;
                return false;
            }
        }
        
        private class RangeIterator : IFastEnumerable<int, int>
        {
            private int _end;
            private int _current;
            
            public RangeIterator(int start, int count)
            {
                _end = unchecked(start + count);
                _current = start;
            }

            public int Start { get; } = 0;
        
            public bool TryGetNext(ref int index, out int value)
            {
                value = _current;
                _current++;
                return value < _end;
            }
        }
    }
    
    public class FastList<T> : IFastList<T, int>
    {
        private T[] _items;
        private int _size;

        public FastList()
        {
            _items = Array.Empty<T>();
        }
        
        private FastList(T[] items)
        {
            _items = items;
            _size = items.Length;
        }
        
        public static IFastList<TElement, int> FromEnumerable<TElement, TEnumerator>(IFastEnumerable<TElement, TEnumerator> source)
        {
            var list = new FastList<TElement>();
            var start = source.Start;
            while (source.TryGetNext(ref start, out var item))
            {
                list.Add(item);
            }

            return list;
        }

        public int Start { get; } = 0;
        
        public bool TryGetNext(ref int index, out T? value)
        {
            if (index >= 0 && index < _items.Length)
            {
                value = _items[index];
                index++;
                return true;
            }

            value = default;
            return false;
        }

        public int Count => _size;
        public bool IsReadOnly { get; } = false;
        
        public void Add(T item)
        {
            T[] array = _items;
            int size = _size;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
            
            void AddWithResize(T item)
            {
                var size = _size;
                Grow(size + 1);
                _size = size + 1;
                _items[size] = item;
            }
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public bool Contains(T item)
        {
            return _size != 0 && IndexOf(item) != -1;
        }

        public void Clear()
        {
            var size = _size;
            _size = 0;
            if (size > 0)
            {
                Array.Clear(_items, 0, size);
            }
        }

        public void CopyTo(T[] array, int index = 0)
        {
            Array.Copy(_items, 0, array, index, _size);
        }

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (_size == _items.Length) Grow(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            
            _items[_size] = default!;
        }
        
        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _size)
                {
                    throw  new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == _items.Length) return;
                if (value > 0)
                {
                    var newItems = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(_items, newItems, _size);
                    }
                    _items = newItems;
                }
                else
                {
                    _items = Array.Empty<T>();
                }
            }
        }
        
        private void Grow(int capacity)
        {
            var newCapacity = _items.Length == 0 ? 4 : 2 * _items.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newCapacity < capacity) newCapacity = capacity;

            Capacity = newCapacity;
        }
    }
}
using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// An independent, deliberately naive reference implementation of multimap semantics, used as
    /// the oracle the real multimaps are compared against.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// <para>
    /// Like <see cref="MultisetModel{T}"/> this type optimises for obviousness, not speed: values
    /// are kept in a plain <see cref="List{T}"/> per key and every member transcribes the contract
    /// documented on <c>IMultiDictionary&lt;TKey,TValue&gt;</c>.
    /// </para>
    /// <para>
    /// The invariant that is easiest to break - and therefore the one worth mirroring exactly - is
    /// that a key exists only while it has at least one value. Every removal path in this model
    /// drops the key as soon as its list empties, so <see cref="KeyCount"/> and
    /// <see cref="TotalValueCount"/> can never disagree about an empty key.
    /// </para>
    /// <para>
    /// <see cref="RemoveRange"/> is modelled as a set operation on purpose: a value repeated inside
    /// the argument removes one occurrence, not two.
    /// </para>
    /// </remarks>
    public sealed class MultimapModel<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> _values;
        private readonly IEqualityComparer<TValue> _valueComparer;
        private readonly bool _allowDuplicateValues;

        /// <summary>
        /// Initializes a new, empty model.
        /// </summary>
        /// <param name="keyComparer">
        /// The key comparer, or <c>null</c> to use <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <param name="valueComparer">
        /// The value comparer, or <c>null</c> to use <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <param name="allowDuplicateValues">
        /// <c>true</c> to store a value more than once under the same key; <c>false</c> to treat
        /// each key as a set.
        /// </param>
        public MultimapModel(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, bool allowDuplicateValues)
        {
            _values = new Dictionary<TKey, List<TValue>>(keyComparer);
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            _allowDuplicateValues = allowDuplicateValues;
        }

        /// <summary>
        /// Gets the number of keys that currently have at least one value.
        /// </summary>
        public int KeyCount => _values.Count;

        /// <summary>
        /// Gets the total number of (key, value) pairs held.
        /// </summary>
        public int TotalValueCount { get; private set; }

        /// <summary>
        /// Gets the keys that currently have at least one value.
        /// </summary>
        public IEnumerable<TKey> Keys => _values.Keys;

        /// <summary>
        /// Gets every value of every key, flattened.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var entry in _values)
                {
                    foreach (var value in entry.Value)
                    {
                        yield return value;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of values stored under the key.
        /// </summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>The number of values, or zero when the key is absent.</returns>
        public int ValueCount(TKey key)
        {
            List<TValue> list;
            return _values.TryGetValue(key, out list) ? list.Count : 0;
        }

        /// <summary>
        /// Returns the values stored under the key.
        /// </summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>The values, or an empty collection when the key is absent.</returns>
        public IReadOnlyCollection<TValue> ValuesOf(TKey key)
        {
            List<TValue> list;
            return _values.TryGetValue(key, out list) ? (IReadOnlyCollection<TValue>)list : new List<TValue>();
        }

        /// <summary>
        /// Adds the value under the key.
        /// </summary>
        /// <param name="key">The key to add under.</param>
        /// <param name="value">The value to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public void Add(TKey key, TValue value)
        {
            if (ReferenceEquals(key, null))
            {
                throw new ArgumentNullException(nameof(key));
            }

            List<TValue> list;
            if (!_values.TryGetValue(key, out list))
            {
                list = new List<TValue>();
                _values.Add(key, list);
            }
            else if (!_allowDuplicateValues && IndexOf(list, value) >= 0)
            {
                return;
            }

            list.Add(value);
            TotalValueCount++;
        }

        /// <summary>
        /// Adds each value of the specified sequence under the key.
        /// </summary>
        /// <param name="key">The key to add under.</param>
        /// <param name="values">The values to add. Each occurrence is honoured.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="values"/> is <c>null</c>.</exception>
        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (var value in values)
            {
                Add(key, value);
            }
        }

        /// <summary>
        /// Determines whether the value is stored under the key.
        /// </summary>
        /// <param name="key">The key to inspect.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when the value is stored under the key.</returns>
        public bool Contains(TKey key, TValue value)
        {
            List<TValue> list;
            return _values.TryGetValue(key, out list) && IndexOf(list, value) >= 0;
        }

        /// <summary>
        /// Determines whether the value is stored under any key.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when at least one key stores the value.</returns>
        public bool ContainsValue(TValue value)
        {
            foreach (var entry in _values)
            {
                foreach (var stored in entry.Value)
                {
                    if (_valueComparer.Equals(stored, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the key together with all of its values.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><c>true</c> when the key was present.</returns>
        public bool Remove(TKey key)
        {
            List<TValue> list;
            if (!_values.TryGetValue(key, out list))
            {
                return false;
            }

            _values.Remove(key);
            TotalValueCount -= list.Count;
            return true;
        }

        /// <summary>
        /// Removes a single occurrence of the value under the key.
        /// </summary>
        /// <param name="key">The key to remove from.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> when an occurrence was removed.</returns>
        public bool Remove(TKey key, TValue value)
        {
            List<TValue> list;
            if (!_values.TryGetValue(key, out list))
            {
                return false;
            }

            var index = IndexOf(list, value);
            if (index < 0)
            {
                return false;
            }

            list.RemoveAt(index);
            TotalValueCount--;

            if (list.Count == 0)
            {
                _values.Remove(key);
            }

            return true;
        }

        /// <summary>
        /// Removes one occurrence of each distinct value of the specified sequence from the key.
        /// </summary>
        /// <param name="key">The key to remove from.</param>
        /// <param name="values">The values to remove, treated as a set.</param>
        /// <returns><c>true</c> when at least one value was removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        public bool RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            List<TValue> list;
            if (!_values.TryGetValue(key, out list))
            {
                return false;
            }

            var removedAny = false;
            var seen = new HashSet<TValue>(_valueComparer);

            foreach (var value in values)
            {
                if (!seen.Add(value))
                {
                    continue;
                }

                var index = IndexOf(list, value);
                if (index < 0)
                {
                    continue;
                }

                list.RemoveAt(index);
                TotalValueCount--;
                removedAny = true;
            }

            if (list.Count == 0)
            {
                _values.Remove(key);
            }

            return removedAny;
        }

        /// <summary>
        /// Removes every key and value.
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            TotalValueCount = 0;
        }

        private int IndexOf(List<TValue> list, TValue value)
        {
            for (var index = 0; index < list.Count; index++)
            {
                if (_valueComparer.Equals(list[index], value))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}

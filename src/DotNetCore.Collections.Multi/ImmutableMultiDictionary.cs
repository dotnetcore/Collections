using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// CS8714: TKey is deliberately unconstrained because null keys of the wrapped
// MultiDictionary<TKey, TValue> follow the wrapped class's own convention (they are
// rejected on Add but enumerable); the "notnull" constraint of the annotated
// net5.0+ reference assemblies is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents an immutable multimap: a dictionary in which one key genuinely owns several
    /// values, which can not be modified after construction. Mutating operations return a new
    /// <see cref="ImmutableMultiDictionary{TKey,TValue}"/> and leave the receiver untouched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type is genuinely thread-safe, not merely synchronized: an instance never changes, so
    /// any number of threads may read it concurrently without locks or fences. Bulk mutation is
    /// expected to go through <see cref="ToBuilder"/> instead of a chain of per-call copies.
    /// </para>
    /// <para>
    /// Structural sharing is observable in two places. <see cref="ToBuilder"/> hands out a builder
    /// that reads the same internal state as the source (no copy is taken), and a builder on which
    /// no write has happened returns the very source instance from <see cref="Builder.ToImmutable"/>
    /// - both are identity checks a caller can perform. The first write copies-on-write, so the
    /// source is safe the moment the builder changes anything.
    /// </para>
    /// <para>
    /// Key, value, comparer and inner-collection semantics are those of
    /// <see cref="MultiDictionary{TKey,TValue}"/> and are delegated to it wholesale, including the
    /// "a key whose value collection has emptied is dropped" invariant.
    /// </para>
    /// </remarks>
    public sealed class ImmutableMultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly MultiDictionary<TKey, TValue> _snapshot;

        /// <summary>
        /// Initializes an empty immutable multimap that allows duplicate values per key.
        /// </summary>
        public ImmutableMultiDictionary() : this((IEqualityComparer<TKey>?)null, true)
        {
        }

        /// <summary>
        /// Initializes an empty immutable multimap with the specified key comparer.
        /// </summary>
        public ImmutableMultiDictionary(IEqualityComparer<TKey>? comparer) : this(comparer, true)
        {
        }

        /// <summary>
        /// Initializes an empty immutable multimap that allows or forbids duplicate values per key.
        /// </summary>
        public ImmutableMultiDictionary(bool allowDuplicateValues) : this(null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes an empty immutable multimap with the specified key comparer and duplicate
        /// values policy. (The mutable type's free-form inner collection factory is not exposed
        /// here; the immutable variants cover the two standard policies - duplicate values kept
        /// or collapsed - and the custom factory remains a <see cref="MultiDictionary{TKey,TValue}"/>
        /// concern.)
        /// </summary>
        public ImmutableMultiDictionary(IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
        {
            _snapshot = new MultiDictionary<TKey, TValue>(comparer, allowDuplicateValues);
        }

        /// <summary>
        /// Initializes the immutable multimap with the given key/value pairs, allowing duplicate
        /// values per key.
        /// </summary>
        public ImmutableMultiDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            : this(pairs, (IEqualityComparer<TKey>?)null, true)
        {
        }

        /// <summary>
        /// Initializes the immutable multimap with the given key/value pairs, key comparer and
        /// duplicate values policy.
        /// </summary>
        public ImmutableMultiDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs, IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
        {
            _snapshot = new MultiDictionary<TKey, TValue>(comparer, allowDuplicateValues);
            if (pairs != null)
            {
                foreach (var pair in pairs)
                {
                    _snapshot.Add(pair.Key, pair.Value);
                }
            }
        }

        internal ImmutableMultiDictionary(MultiDictionary<TKey, TValue> snapshot)
        {
            _snapshot = snapshot;
        }

        /// <summary>
        /// Rebuilds an immutable multimap from a data model produced by
        /// <see cref="ToSerializableModel"/>. The comparer and the duplicate-values policy are
        /// configuration, so they are supplied here rather than carried by the model. Validation
        /// follows <see cref="MultiDictionary{TKey,TValue}.FromModel"/>.
        /// </summary>
        public static ImmutableMultiDictionary<TKey, TValue> FromModel(
            MultiDictionaryModel<TKey, TValue> model, IEqualityComparer<TKey>? comparer = null,
            bool allowDuplicateValues = true)
        {
            return new ImmutableMultiDictionary<TKey, TValue>(
                MultiDictionary<TKey, TValue>.FromModel(model, comparer, allowDuplicateValues));
        }

        /// <summary>
        /// Gets the comparer that determines key equality.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => _snapshot.Comparer;

        /// <summary>
        /// Gets the number of distinct keys stored.
        /// </summary>
        public int Count => _snapshot.Count;

        /// <summary>
        /// Gets the number of distinct keys stored. Alias of <see cref="Count"/>, named the way
        /// <see cref="MultiDictionary{TKey,TValue}.KeyCount"/> is.
        /// </summary>
        public int KeyCount => _snapshot.KeyCount;

        /// <summary>
        /// Gets the total number of values stored across all keys.
        /// </summary>
        public int TotalValueCount => _snapshot.TotalValueCount;

        /// <summary>
        /// Gets the value collection stored for the given key. An absent key yields an empty
        /// collection (never <c>null</c> and never an exception), mirroring
        /// <see cref="MultiDictionary{TKey,TValue}.this[TKey]"/>.
        /// </summary>
        public IReadOnlyCollection<TValue> this[TKey key] => _snapshot[key];

        /// <summary>
        /// Gets the number of values stored for the given key (0 when the key is absent).
        /// </summary>
        public int ValueCount(TKey key)
        {
            return _snapshot.ValueCount(key);
        }

        /// <summary>
        /// Enumerates the stored keys.
        /// </summary>
        public IEnumerable<TKey> Keys => _snapshot.Keys;

        /// <summary>
        /// Enumerates all stored values, key by key.
        /// </summary>
        public IEnumerable<TValue> Values => _snapshot.Values;

        /// <summary>
        /// Returns a new immutable multimap with the given value appended to the key's value
        /// collection. A duplicate value on a key that forbids them throws
        /// <see cref="ArgumentException"/>, mirroring <see cref="MultiDictionary{TKey,TValue}.Add"/>.
        /// </summary>
        public ImmutableMultiDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            var copy = _snapshot.Clone();
            copy.Add(key, value);
            return new ImmutableMultiDictionary<TKey, TValue>(copy);
        }

        /// <summary>
        /// Returns a new immutable multimap with the given values appended to the key's value
        /// collection.
        /// </summary>
        public ImmutableMultiDictionary<TKey, TValue> AddRange(TKey key, IEnumerable<TValue> values)
        {
            var copy = _snapshot.Clone();
            copy.AddRange(key, values);
            return new ImmutableMultiDictionary<TKey, TValue>(copy);
        }

        /// <summary>
        /// Returns a new immutable multimap with the given key (and all its values) removed, or
        /// the receiver itself when the key is absent - an immutable instance may safely be
        /// shared, so nothing needs to be copied to represent "no change".
        /// </summary>
        public ImmutableMultiDictionary<TKey, TValue> Remove(TKey key)
        {
            if (!ContainsKey(key))
            {
                return this;
            }

            var copy = _snapshot.Clone();
            copy.Remove(key);
            return new ImmutableMultiDictionary<TKey, TValue>(copy);
        }

        /// <summary>
        /// Returns a new immutable multimap with one occurrence of the given value removed from
        /// the key's value collection, or the receiver itself when nothing would change; a key
        /// whose collection has emptied is dropped, as everywhere else.
        /// </summary>
        public ImmutableMultiDictionary<TKey, TValue> Remove(TKey key, TValue value)
        {
            if (!Contains(key, value))
            {
                return this;
            }

            var copy = _snapshot.Clone();
            copy.Remove(key, value);
            return new ImmutableMultiDictionary<TKey, TValue>(copy);
        }

        /// <summary>
        /// Returns a new immutable multimap in which one occurrence of each distinct argument
        /// value is removed from the key's value collection, or the receiver itself when nothing
        /// would change.
        /// </summary>
        public ImmutableMultiDictionary<TKey, TValue> RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            var copy = _snapshot.Clone();
            copy.RemoveRange(key, values);
            return new ImmutableMultiDictionary<TKey, TValue>(copy);
        }

        /// <summary>
        /// Returns a new empty immutable multimap with the same comparer and duplicate values
        /// policy (taken from a cleared clone of the current snapshot, which carries the
        /// configuration).
        /// </summary>
        public ImmutableMultiDictionary<TKey, TValue> Clear()
        {
            var copy = _snapshot.Clone();
            copy.Clear();
            return new ImmutableMultiDictionary<TKey, TValue>(copy);
        }

        /// <summary>
        /// Determines whether the given key is stored.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return _snapshot.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the given key holds at least one occurrence of the given value.
        /// </summary>
        public bool Contains(TKey key, TValue value)
        {
            return _snapshot.Contains(key, value);
        }

        /// <summary>
        /// Determines whether any key holds at least one occurrence of the given value.
        /// </summary>
        public bool ContainsValue(TValue value)
        {
            return _snapshot.ContainsValue(value);
        }

        /// <summary>
        /// Gets the value collection stored for the given key.
        /// </summary>
        public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
        {
            return _snapshot.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an <see cref="ILookup{TKey,TElement}"/> view of the multimap.
        /// </summary>
        public ILookup<TKey, TValue> AsLookup()
        {
            return _snapshot.AsLookup();
        }

        /// <summary>
        /// Exports the multimap as a snapshot dictionary from key to its value collection. The
        /// inner collections are live views; the outer dictionary is independent.
        /// </summary>
        public IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> ToDictionary()
        {
            return _snapshot.ToDictionary();
        }

        /// <summary>
        /// Exports the multimap as a plain data model for external serialization.
        /// </summary>
        public MultiDictionaryModel<TKey, TValue> ToSerializableModel()
        {
            return _snapshot.ToSerializableModel();
        }

        /// <summary>
        /// Returns a builder pre-loaded with the contents of this multimap. No copy of the data is
        /// taken: the builder reads the same state as this instance until its first write
        /// (copy-on-write), so an untouched builder freezes back to this very instance.
        /// </summary>
        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        /// <summary>
        /// Exports the multimap as an equivalent <see cref="MultiDictionary{TKey,TValue}"/>
        /// snapshot, independent of this instance.
        /// </summary>
        public MultiDictionary<TKey, TValue> ToMultiDictionary()
        {
            return _snapshot.Clone();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _snapshot.ToString();
        }

        /// <summary>
        /// A mutable builder for an <see cref="ImmutableMultiDictionary{TKey,TValue}"/>. The
        /// builder is the cheap way to produce a new immutable multimap in several steps: it works
        /// on private state from the first write on and never touches the instance it was created
        /// from.
        /// </summary>
        /// <remarks>
        /// A builder is not thread-safe; it is meant to be owned by one thread and handed the
        /// resulting immutable multimap to others. Reading a builder while writing to it from
        /// another thread is not supported.
        /// </remarks>
        public sealed class Builder
        {
            private MultiDictionary<TKey, TValue> _working;
            private ImmutableMultiDictionary<TKey, TValue>? _source;
            private bool _copied;

            /// <summary>
            /// Initializes an empty builder that allows duplicate values per key.
            /// </summary>
            public Builder() : this((IEqualityComparer<TKey>?)null, true)
            {
            }

            /// <summary>
            /// Initializes an empty builder with the specified key comparer.
            /// </summary>
            public Builder(IEqualityComparer<TKey>? comparer) : this(comparer, true)
            {
            }

            /// <summary>
            /// Initializes an empty builder that allows or forbids duplicate values per key.
            /// </summary>
            public Builder(bool allowDuplicateValues) : this((IEqualityComparer<TKey>?)null, allowDuplicateValues)
            {
            }

            /// <summary>
            /// Initializes an empty builder with the specified key comparer and duplicate values
            /// policy.
            /// </summary>
            public Builder(IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
            {
                _working = new MultiDictionary<TKey, TValue>(comparer, allowDuplicateValues);
                _copied = true;
            }

            /// <summary>
            /// Initializes a builder pre-loaded with the given key/value pairs.
            /// </summary>
            public Builder(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
                : this(pairs, (IEqualityComparer<TKey>?)null, true)
            {
            }

            private Builder(
                IEnumerable<KeyValuePair<TKey, TValue>>? pairs, IEqualityComparer<TKey>? comparer,
                bool allowDuplicateValues)
            {
                _working = new MultiDictionary<TKey, TValue>(comparer, allowDuplicateValues);
                if (pairs != null)
                {
                    foreach (var pair in pairs)
                    {
                        _working.Add(pair.Key, pair.Value);
                    }
                }

                _copied = true;
            }

            private Builder(MultiDictionary<TKey, TValue> working, bool copied)
            {
                _working = working;
                _copied = copied;
            }

            internal Builder(ImmutableMultiDictionary<TKey, TValue> source)
            {
                _source = source;
                _working = source._snapshot;
                _copied = false;
            }

            /// <summary>
            /// Gets the comparer that determines key equality.
            /// </summary>
            public IEqualityComparer<TKey> Comparer => _working.Comparer;

            /// <summary>
            /// Gets the number of distinct keys stored.
            /// </summary>
            public int Count => _working.Count;

            /// <summary>
            /// Gets the total number of values stored across all keys.
            /// </summary>
            public int TotalValueCount => _working.TotalValueCount;

            /// <summary>
            /// Appends the given value to the key's value collection.
            /// </summary>
            public void Add(TKey key, TValue value)
            {
                EnsureOwn();
                _working.Add(key, value);
            }

            /// <summary>
            /// Appends the given values to the key's value collection.
            /// </summary>
            public void AddRange(TKey key, IEnumerable<TValue> values)
            {
                EnsureOwn();
                _working.AddRange(key, values);
            }

            /// <summary>
            /// Removes the given key and all its values, and returns whether the key was present.
            /// </summary>
            public bool Remove(TKey key)
            {
                EnsureOwn();
                return _working.Remove(key);
            }

            /// <summary>
            /// Removes one occurrence of the given value from the key's value collection, and
            /// returns whether an occurrence was removed.
            /// </summary>
            public bool Remove(TKey key, TValue value)
            {
                EnsureOwn();
                return _working.Remove(key, value);
            }

            /// <summary>
            /// Removes one occurrence of each distinct argument value from the key's value
            /// collection, and returns whether anything was removed.
            /// </summary>
            public bool RemoveRange(TKey key, IEnumerable<TValue> values)
            {
                EnsureOwn();
                return _working.RemoveRange(key, values);
            }

            /// <summary>
            /// Removes all keys and values.
            /// </summary>
            public void Clear()
            {
                EnsureOwn();
                _working.Clear();
            }

            /// <summary>
            /// Determines whether the given key is stored.
            /// </summary>
            public bool ContainsKey(TKey key)
            {
                return _working.ContainsKey(key);
            }

            /// <summary>
            /// Gets the number of values stored for the given key (0 when the key is absent).
            /// </summary>
            public int ValueCount(TKey key)
            {
                return _working.ValueCount(key);
            }

            /// <summary>
            /// Determines whether the given key holds at least one occurrence of the given value.
            /// </summary>
            public bool Contains(TKey key, TValue value)
            {
                return _working.Contains(key, value);
            }

            /// <summary>
            /// Gets the value collection stored for the given key.
            /// </summary>
            public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
            {
                return _working.TryGetValue(key, out value);
            }

            /// <summary>
            /// Freezes the builder's current state into an immutable multimap. When no write has
            /// happened since the builder was created from an immutable multimap, that very
            /// instance is returned - the freeze is then a plain identity check, which is how the
            /// structural sharing between the two is made observable. A standalone (or
            /// already-written) builder hands its working state to the frozen instance and
            /// continues on a private copy, so writes after the freeze are invisible to it.
            /// </summary>
            public ImmutableMultiDictionary<TKey, TValue> ToImmutable()
            {
                if (!_copied && _source != null)
                {
                    return _source;
                }

                var frozen = new ImmutableMultiDictionary<TKey, TValue>(_working);
                _working = _working.Clone();
                return frozen;
            }

            /// <summary>
            /// Creates an independent copy of the builder: elements written afterwards to either
            /// builder are invisible to the other.
            /// </summary>
            public Builder Clone()
            {
                EnsureOwn();
                return new Builder(_working.Clone(), true);
            }

            private void EnsureOwn()
            {
                if (_copied)
                {
                    return;
                }

                _working = _working.Clone();
                _copied = true;
                _source = null;
            }
        }
    }
}

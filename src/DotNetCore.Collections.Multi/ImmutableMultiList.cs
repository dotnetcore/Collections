using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: T is deliberately unconstrained because null elements are supported
// (tracked in a dedicated bucket by the wrapped MultiList<T>); the "notnull"
// constraint of the annotated net5.0+ reference assemblies is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents an immutable multiset (bag): an unordered collection that allows duplicate
    /// elements and tracks the number of occurrences (copies) of each element, which can not be
    /// modified after construction. Mutating operations return a new
    /// <see cref="ImmutableMultiList{T}"/> and leave the receiver untouched.
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
    /// Element and copy-count semantics are those of <see cref="MultiList{T}"/> and are delegated
    /// to it wholesale; the comparer and null-element handling are identical.
    /// </para>
    /// </remarks>
    public sealed class ImmutableMultiList<T> : IEnumerable<T>, IReadOnlyCollection<T>, IEquatable<ImmutableMultiList<T>>
    {
        private readonly MultiList<T> _snapshot;

        /// <summary>
        /// Initializes an empty immutable multiset.
        /// </summary>
        public ImmutableMultiList() : this((IEnumerable<T>?)null, null)
        {
        }

        /// <summary>
        /// Initializes an empty immutable multiset with the specified element comparer.
        /// </summary>
        public ImmutableMultiList(IEqualityComparer<T>? comparer) : this((IEnumerable<T>?)null, comparer)
        {
        }

        /// <summary>
        /// Initializes the immutable multiset with the copies of the specified collection.
        /// </summary>
        public ImmutableMultiList(IEnumerable<T> collection) : this(collection, null)
        {
        }

        /// <summary>
        /// Initializes the immutable multiset with the copies of the specified collection and
        /// element comparer.
        /// </summary>
        public ImmutableMultiList(IEnumerable<T>? collection, IEqualityComparer<T>? comparer)
        {
            _snapshot = collection == null ? new MultiList<T>(comparer) : new MultiList<T>(collection, comparer);
        }

        internal ImmutableMultiList(MultiList<T> snapshot)
        {
            _snapshot = snapshot;
        }

        /// <summary>
        /// Rebuilds an immutable multiset from a data model produced by
        /// <see cref="ToSerializableModel"/>. The comparer is configuration, so it is supplied
        /// here rather than carried by the model; elements the comparer calls equal are merged in
        /// the multiset reading. Validation follows <see cref="MultiList{T}.FromModel"/>.
        /// </summary>
        public static ImmutableMultiList<T> FromModel(MultiListModel<T> model, IEqualityComparer<T>? comparer = null)
        {
            return new ImmutableMultiList<T>(MultiList<T>.FromModel(model, comparer));
        }

        /// <summary>
        /// Gets the comparer that determines element equality.
        /// </summary>
        public IEqualityComparer<T> Comparer => _snapshot.Comparer;

        /// <summary>
        /// Gets the total number of elements stored, duplicates included.
        /// </summary>
        public int TotalCount => _snapshot.TotalCount;

        /// <summary>
        /// Gets the number of distinct elements stored (copies are counted once).
        /// </summary>
        public int DistinctCount => _snapshot.DistinctCount;

        /// <summary>
        /// Gets the total number of elements stored. Alias of <see cref="TotalCount"/>, present
        /// for <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        public int Count => _snapshot.TotalCount;

        /// <summary>
        /// Returns a new immutable multiset with the given element added once.
        /// </summary>
        public ImmutableMultiList<T> Add(T item)
        {
            var copy = _snapshot.Clone();
            copy.Add(item);
            return new ImmutableMultiList<T>(copy);
        }

        /// <summary>
        /// Returns a new immutable multiset with the given element added the specified number of
        /// times. A non-positive <paramref name="times"/> throws
        /// <see cref="ArgumentOutOfRangeException"/>, mirroring <see cref="MultiList{T}.Add(T, int)"/>.
        /// </summary>
        public ImmutableMultiList<T> Add(T item, int times)
        {
            var copy = _snapshot.Clone();
            copy.Add(item, times);
            return new ImmutableMultiList<T>(copy);
        }

        /// <summary>
        /// Returns a new immutable multiset with the copies of the specified collection added.
        /// </summary>
        public ImmutableMultiList<T> AddRange(IEnumerable<T> items)
        {
            var copy = _snapshot.Clone();
            copy.AddRange(items);
            return new ImmutableMultiList<T>(copy);
        }

        /// <summary>
        /// Returns a new immutable multiset with every copy of the given element removed, or the
        /// receiver itself when the element is absent - an immutable instance may safely be
        /// shared, so nothing needs to be copied to represent "no change".
        /// </summary>
        public ImmutableMultiList<T> RemoveAllCopies(T item)
        {
            if (CountOf(item) == 0)
            {
                return this;
            }

            var copy = _snapshot.Clone();
            copy.RemoveAllCopies(item);
            return new ImmutableMultiList<T>(copy);
        }

        /// <summary>
        /// Returns a new immutable multiset with the specified number of copies of the given
        /// element removed, or the receiver itself when nothing would change.
        /// </summary>
        public ImmutableMultiList<T> Remove(T item, int times)
        {
            if (CountOf(item) == 0)
            {
                return this;
            }

            var copy = _snapshot.Clone();
            copy.Remove(item, times);
            return new ImmutableMultiList<T>(copy);
        }

        /// <summary>
        /// Returns a new empty immutable multiset with the same comparer.
        /// </summary>
        public ImmutableMultiList<T> Clear()
        {
            return new ImmutableMultiList<T>(new MultiList<T>(_snapshot.Comparer));
        }

        /// <summary>
        /// Returns a new immutable multiset with identical contents compared under the specified
        /// element comparer instead.
        /// </summary>
        public ImmutableMultiList<T> WithComparer(IEqualityComparer<T>? comparer)
        {
            var copy = _snapshot.Clone();
            return new ImmutableMultiList<T>(Rekeyed(copy, comparer));
        }

        private static MultiList<T> Rekeyed(MultiList<T> copy, IEqualityComparer<T>? comparer)
        {
            // Rebuild under the new comparer unless it is already the same notion of equality.
            if (EqualityComparer<IEqualityComparer<T>>.Default.Equals(copy.Comparer, comparer ?? EqualityComparer<T>.Default))
            {
                return copy;
            }

            return new MultiList<T>(copy, comparer);
        }

        /// <summary>
        /// Determines whether the multiset contains at least one copy of the given element.
        /// </summary>
        public bool Contains(T item)
        {
            return _snapshot.Contains(item);
        }

        /// <summary>
        /// Determines whether the multiset contains at least one copy of every element of the
        /// given collection.
        /// </summary>
        public bool ContainsAll(IEnumerable<T> items)
        {
            return _snapshot.ContainsAll(items);
        }

        /// <summary>
        /// Gets the number of copies of the given element stored in the multiset.
        /// </summary>
        public int CountOf(T item)
        {
            return _snapshot.CountOf(item);
        }

        /// <summary>
        /// Enumerates the distinct elements with their copy counts.
        /// </summary>
        public IEnumerable<(T Item, int Count)> EntrySet()
        {
            return _snapshot.EntrySet();
        }

        /// <summary>
        /// Enumerates the distinct elements once each.
        /// </summary>
        public IEnumerable<T> DistinctItems()
        {
            return _snapshot.DistinctItems();
        }

        /// <summary>
        /// Copies the elements (duplicates included) into the given array.
        /// </summary>
        public T[] ToArray()
        {
            return _snapshot.ToArray();
        }

        /// <summary>
        /// Copies the elements (duplicates included) into a list.
        /// </summary>
        public List<T> ToList()
        {
            return _snapshot.ToList();
        }

        /// <summary>
        /// Exports the multiset as a snapshot dictionary of element to copy count. Throws
        /// <see cref="InvalidOperationException"/> when the multiset contains a <c>null</c>
        /// element.
        /// </summary>
        public IReadOnlyDictionary<T, int> ToDictionary()
        {
            return _snapshot.ToDictionary();
        }

        /// <summary>
        /// Exports the multiset as a plain data model for external serialization.
        /// </summary>
        public MultiListModel<T> ToSerializableModel()
        {
            return _snapshot.ToSerializableModel();
        }

        /// <summary>
        /// Determines whether every element of the multiset is present in the given collection
        /// with at least as many copies, using multiset semantics.
        /// </summary>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _snapshot.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether every element of the given collection is present in the multiset
        /// with at least as many copies, using multiset semantics.
        /// </summary>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _snapshot.IsSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the multiset is a subset of the given collection that is not equal
        /// as a multiset.
        /// </summary>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _snapshot.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the multiset is a superset of the given collection that is not equal
        /// as a multiset.
        /// </summary>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _snapshot.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the multiset and the given collection share at least one element.
        /// </summary>
        public bool Overlaps(IEnumerable<T> other)
        {
            return _snapshot.Overlaps(other);
        }

        /// <summary>
        /// Determines whether the two multisets hold every distinct element with the same number
        /// of copies.
        /// </summary>
        public bool Equals(ImmutableMultiList<T>? other)
        {
            return other != null && _snapshot.Equals(other._snapshot);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ImmutableMultiList<T> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _snapshot.GetHashCode();
        }

        /// <summary>
        /// Returns a builder pre-loaded with the contents of this multiset. No copy of the data is
        /// taken: the builder reads the same state as this instance until its first write
        /// (copy-on-write), so an untouched builder freezes back to this very instance.
        /// </summary>
        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        /// <summary>
        /// Exports the multiset as an equivalent <see cref="MultiList{T}"/> snapshot, independent
        /// of this instance.
        /// </summary>
        public MultiList<T> ToMultiList()
        {
            return _snapshot.Clone();
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
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
        /// A mutable builder for an <see cref="ImmutableMultiList{T}"/>. The builder is the cheap
        /// way to produce a new immutable multiset in several steps: it works on private state
        /// from the first write on and never touches the instance it was created from.
        /// </summary>
        /// <remarks>
        /// A builder is not thread-safe; it is meant to be owned by one thread and handed the
        /// resulting immutable multiset to others. Reading a builder while writing to it from
        /// another thread is not supported.
        /// </remarks>
        public sealed class Builder
        {
            private MultiList<T> _working;
            private ImmutableMultiList<T>? _source;
            private bool _copied;

            /// <summary>
            /// Initializes an empty builder.
            /// </summary>
            public Builder() : this(null, null)
            {
            }

            /// <summary>
            /// Initializes an empty builder with the specified element comparer.
            /// </summary>
            public Builder(IEqualityComparer<T>? comparer) : this(null, comparer)
            {
            }

            /// <summary>
            /// Initializes a builder pre-loaded with the copies of the specified collection.
            /// </summary>
            public Builder(IEnumerable<T> collection) : this(collection, null)
            {
            }

            /// <summary>
            /// Initializes a builder pre-loaded with the copies of the specified collection and
            /// element comparer.
            /// </summary>
            public Builder(IEnumerable<T>? collection, IEqualityComparer<T>? comparer)
            {
                _working = collection == null ? new MultiList<T>(comparer) : new MultiList<T>(collection, comparer);
                _copied = true;
            }

            internal Builder(ImmutableMultiList<T> source)
            {
                _source = source;
                _working = source._snapshot;
                _copied = false;
            }

            /// <summary>
            /// Gets the comparer that determines element equality.
            /// </summary>
            public IEqualityComparer<T> Comparer => _working.Comparer;

            /// <summary>
            /// Gets the total number of elements stored, duplicates included.
            /// </summary>
            public int TotalCount => _working.TotalCount;

            /// <summary>
            /// Gets the number of distinct elements stored (copies are counted once).
            /// </summary>
            public int DistinctCount => _working.DistinctCount;

            /// <summary>
            /// Adds the given element once.
            /// </summary>
            public void Add(T item)
            {
                EnsureOwn();
                _working.Add(item);
            }

            /// <summary>
            /// Adds the given element the specified number of times. A non-positive
            /// <paramref name="times"/> throws <see cref="ArgumentOutOfRangeException"/>.
            /// </summary>
            public void Add(T item, int times)
            {
                EnsureOwn();
                _working.Add(item, times);
            }

            /// <summary>
            /// Adds the copies of the specified collection.
            /// </summary>
            public void AddRange(IEnumerable<T> items)
            {
                EnsureOwn();
                _working.AddRange(items);
            }

            /// <summary>
            /// Removes the specified number of copies of the given element and returns how many
            /// copies were actually removed.
            /// </summary>
            public int Remove(T item, int times)
            {
                EnsureOwn();
                return _working.Remove(item, times);
            }

            /// <summary>
            /// Removes every copy of the given element and returns how many copies were removed.
            /// </summary>
            public bool RemoveAllCopies(T item)
            {
                EnsureOwn();
                return _working.RemoveAllCopies(item);
            }

            /// <summary>
            /// Removes all elements.
            /// </summary>
            public void Clear()
            {
                EnsureOwn();
                _working.Clear();
            }

            /// <summary>
            /// Determines whether the builder contains at least one copy of the given element.
            /// </summary>
            public bool Contains(T item)
            {
                return _working.Contains(item);
            }

            /// <summary>
            /// Gets the number of copies of the given element stored in the builder.
            /// </summary>
            public int CountOf(T item)
            {
                return _working.CountOf(item);
            }

            /// <summary>
            /// Enumerates the distinct elements with their copy counts.
            /// </summary>
            public IEnumerable<(T Item, int Count)> EntrySet()
            {
                return _working.EntrySet();
            }

            /// <summary>
            /// Freezes the builder's current state into an immutable multiset. When no write has
            /// happened since the builder was created from an immutable multiset, that very
            /// instance is returned - the freeze is then a plain identity check, which is how the
            /// structural sharing between the two is made observable. A standalone (or
            /// already-written) builder hands its working state to the frozen instance and
            /// continues on a private copy, so writes after the freeze are invisible to it.
            /// </summary>
            public ImmutableMultiList<T> ToImmutable()
            {
                if (!_copied && _source != null)
                {
                    return _source;
                }

                var frozen = new ImmutableMultiList<T>(_working);
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
                return new Builder { _working = _working.Clone(), _copied = true };
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>GroupAdjacent</c> operator of the P2 layer. It groups <em>runs</em> of elements that
    /// share a key, rather than all elements that share it, so <c>[1, 1, 2, 1]</c> produces three
    /// groups and not two.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the operator for "consecutive spans": consecutive days in the same month, adjacent
    /// log lines with the same level, a sorted sequence split into equal-key blocks. When the
    /// grouping key is not monotonic the answer is deliberately different from <c>GroupBy</c>, and
    /// that difference is the reason the operator exists.
    /// </para>
    /// <para>
    /// Each group is materialised before it is yielded, because its size is unknown until the run
    /// ends. A consumer that stops mid-group therefore still pays for the whole run, but never for
    /// the rest of the source.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being grouped.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    public readonly struct GroupAdjacentValueEnumerable<TSource, TEnumerator, T, TKey> : IValueEnumerable<IGrouping<TKey, T>, GroupAdjacentValueEnumerator<TEnumerator, T, TKey>>, IValueEnumerableHooks<IGrouping<TKey, T>>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal GroupAdjacentValueEnumerable(TSource source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that walks the runs of equal keys.</summary>
        /// <returns>An enumerator positioned before the first group.</returns>
        public GroupAdjacentValueEnumerator<TEnumerator, T, TKey> GetEnumerator()
            => new GroupAdjacentValueEnumerator<TEnumerator, T, TKey>(_source.GetEnumerator(), _keySelector, _comparer);

        IEnumerator<IGrouping<TKey, T>> IEnumerable<IGrouping<TKey, T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The number of runs is only known after walking the source.
        bool IValueEnumerableHooks<IGrouping<TKey, T>>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<IGrouping<TKey, T>>.TryGetSpan(out ReadOnlySpan<IGrouping<TKey, T>> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<IGrouping<TKey, T>>.TryCopyTo(Span<IGrouping<TKey, T>> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Accumulates one run at a time. The element that ended the run is held in a pending slot, so
    /// the next pull starts from it instead of reading the source again.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    public struct GroupAdjacentValueEnumerator<TEnumerator, T, TKey> : IEnumerator<IGrouping<TKey, T>>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private IGrouping<TKey, T>? _current;
        private T _pending;
        private bool _hasPending;

        internal GroupAdjacentValueEnumerator(TEnumerator enumerator, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _enumerator = enumerator;
            _keySelector = keySelector;
            _comparer = comparer;
            _current = null;
            _pending = default!;
            _hasPending = false;
        }

        /// <summary>Gets the current run.</summary>
        public IGrouping<TKey, T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next run of equal keys.</summary>
        /// <returns><see langword="true"/> if a run was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            T first;
            if (_hasPending)
            {
                first = _pending;
                _hasPending = false;
            }
            else if (_enumerator.MoveNext())
            {
                first = _enumerator.Current;
            }
            else
            {
                return false;
            }

            var key = _keySelector(first);
            var items = new List<T> { first };

            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
                if (!_comparer.Equals(key, _keySelector(candidate)))
                {
                    // The run ends here. Hold the element so the next pull starts with it rather
                    // than skipping it.
                    _pending = candidate;
                    _hasPending = true;
                    break;
                }

                items.Add(candidate);
            }

            _current = new AdjacentGrouping<TKey, T>(key, items.ToArray());
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first run.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _current = null;
            _pending = default!;
            _hasPending = false;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }

    /// <summary>
    /// One run produced by <c>GroupAdjacent</c>. It is a class rather than a struct because
    /// <see cref="IGrouping{TKey,T}"/> is an interface and every group is handed out as one; a
    /// struct would box on the way out anyway.
    /// </summary>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class AdjacentGrouping<TKey, T> : IGrouping<TKey, T>
    {
        private readonly TKey _key;
        private readonly T[] _items;

        internal AdjacentGrouping(TKey key, T[] items)
        {
            _key = key;
            _items = items;
        }

        /// <summary>Gets the key shared by every element of the run.</summary>
        public TKey Key => _key;

        /// <summary>Returns an enumerator over the elements of the run.</summary>
        /// <returns>An enumerator over the materialised run.</returns>
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}

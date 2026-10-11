#if !NET10_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>RightJoin</c> operator of the P0 polyfill layer. It is <c>LeftJoin</c> with the two
    /// sides swapped: the inner sequence drives the result and the outer sequence is the one folded
    /// into the lookup, so an inner element with no match is projected against
    /// <see langword="default"/> rather than dropped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>RightJoin</c> is a BCL operator from .NET 10 onwards, and like <c>LeftJoin</c> it is
    /// polyfilled on the lower targets only, with no accelerated override above.
    /// </para>
    /// <para>
    /// The operator is deferred. Nothing is read when the pipeline is built; on the first pull the
    /// outer sequence is folded into a hash lookup and the inner sequence is then streamed past it.
    /// </para>
    /// <para>
    /// The shape of the answer is the mirror of <c>LeftJoin</c>'s and is the BCL's: inner order is
    /// the primary order, and an inner element that matches several outer elements produces one
    /// result per match, in outer order. An inner element with no match produces exactly one result,
    /// and the outer side of that result is <see langword="default"/>.
    /// </para>
    /// <para>
    /// An element whose key is <see langword="null"/> never matches, on either side, which is the
    /// rule the BCL's lookup applies as well.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable supplying the outer sequence.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
    /// <typeparam name="TInner">The type of the inner elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
    /// <typeparam name="TResult">The type of the projected results.</typeparam>
    public readonly struct RightJoinValueEnumerable<TSource, TEnumerator, TOuter, TInner, TKey, TResult> : IValueEnumerable<TResult, RightJoinValueEnumerator<TSource, TEnumerator, TOuter, TInner, TKey, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<TOuter, TEnumerator>
        where TEnumerator : struct, IEnumerator<TOuter>
    {
        private readonly TSource _outer;
        private readonly IEnumerable<TInner> _inner;
        private readonly Func<TOuter, TKey> _outerKeySelector;
        private readonly Func<TInner, TKey> _innerKeySelector;
        private readonly Func<TOuter, TInner, TResult> _resultSelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal RightJoinValueEnumerable(TSource outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            _outer = outer;
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _outerKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
            _innerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that joins the two sequences the other way round.</summary>
        /// <returns>An enumerator positioned before the first result.</returns>
        public RightJoinValueEnumerator<TSource, TEnumerator, TOuter, TInner, TKey, TResult> GetEnumerator()
            => new RightJoinValueEnumerator<TSource, TEnumerator, TOuter, TInner, TKey, TResult>(_outer, _inner, _outerKeySelector, _innerKeySelector, _resultSelector, _comparer);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // An inner element can produce any number of results - none of the outer count, inner count
        // or distinct-key count answers how many, and the results are not one contiguous block.
        bool IValueEnumerableHooks<TResult>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<TResult>.TryGetSpan(out ReadOnlySpan<TResult> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<TResult>.TryCopyTo(Span<TResult> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Streams the inner sequence past a hash lookup of the outer one, projecting one result per
    /// match and one against <see langword="default"/> when there is none.
    /// </summary>
    /// <typeparam name="TSource">The value enumerable supplying the outer sequence.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
    /// <typeparam name="TInner">The type of the inner elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
    /// <typeparam name="TResult">The type of the projected results.</typeparam>
    public struct RightJoinValueEnumerator<TSource, TEnumerator, TOuter, TInner, TKey, TResult> : IEnumerator<TResult>
        where TSource : struct, IValueEnumerable<TOuter, TEnumerator>
        where TEnumerator : struct, IEnumerator<TOuter>
    {
        private readonly TSource _outer;
        private IEnumerator<TInner> _inner;
        private readonly Func<TOuter, TKey> _outerKeySelector;
        private readonly Func<TInner, TKey> _innerKeySelector;
        private readonly Func<TOuter, TInner, TResult> _resultSelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private JoinLookup<TKey, TOuter>? _lookup;
        private List<TOuter>? _matches;
        private int _matchIndex;
        private TInner _currentInner;
        private TResult _current;

        internal RightJoinValueEnumerator(TSource outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            _outer = outer;
            _inner = inner.GetEnumerator();
            _outerKeySelector = outerKeySelector;
            _innerKeySelector = innerKeySelector;
            _resultSelector = resultSelector;
            _comparer = comparer;
            _lookup = null;
            _matches = null;
            _matchIndex = 0;
            _currentInner = default!;
            _current = default!;
        }

        /// <summary>Gets the current result.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // TResult is unconstrained, so the result is legitimately null-able whenever TResult is a
        // reference type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next result.</summary>
        /// <returns><see langword="true"/> if a result was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            // A match list that has not been drained keeps the inner element it belongs to.
            var matches = _matches;
            if (matches != null && _matchIndex < matches.Count)
            {
                _current = _resultSelector(matches[_matchIndex], _currentInner);
                _matchIndex++;
                return true;
            }

            while (_inner.MoveNext())
            {
                var inner = _inner.Current;

                // The lookup is built on the first pull, and only once an inner element exists -
                // an empty inner sequence never touches the outer one.
                var lookup = _lookup;
                if (lookup is null)
                {
                    lookup = BuildLookup();
                    _lookup = lookup;
                }

                var key = _innerKeySelector(inner);
                if (!(key is null) && lookup.TryGetGroup(key, out var group))
                {
                    var groupElements = group!;
                    _currentInner = inner;
                    _matches = groupElements;
                    _matchIndex = 1;
                    _current = _resultSelector(groupElements[0], inner);
                    return true;
                }

                // An unmatched inner element is projected against default, not dropped.
                _current = _resultSelector(default!, inner);
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first result.</summary>
        public void Reset()
        {
            _inner.Reset();
            _lookup = null;
            _matches = null;
            _matchIndex = 0;
            _currentInner = default!;
            _current = default!;
        }

        /// <summary>Releases the inner enumerator, which is the one this operator owns.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _inner.Dispose();

        private JoinLookup<TKey, TOuter> BuildLookup()
        {
            var lookup = new JoinLookup<TKey, TOuter>(_comparer);
            foreach (var outer in _outer)
            {
                var key = _outerKeySelector(outer);
                if (!(key is null))
                {
                    lookup.Add(key, outer);
                }
            }

            return lookup;
        }
    }
}
#endif

#if !NET10_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>LeftJoin</c> operator of the P0 polyfill layer. It walks the outer sequence, finds
    /// the inner elements that share each outer element's key, and projects the pair; an outer
    /// element with no match is projected against <see langword="default"/> rather than dropped,
    /// which is what makes this an outer join and not a join.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>LeftJoin</c> is a BCL operator from .NET 10 onwards, and it is the one operator of the
    /// layer that is polyfilled on the lower targets only. There is deliberately no accelerated
    /// override above it: .NET 10 declares the same signature, so a second one in this package
    /// would only compete with the platform's.
    /// </para>
    /// <para>
    /// The operator is deferred but not streaming on the lookup side. Nothing is read when the
    /// pipeline is built; on the first pull the inner sequence is folded into a hash lookup, and
    /// the outer sequence is then streamed past it. Building the lookup costs one full pass over
    /// the inner sequence, so a consumer that stops after one result still paid for it - but the
    /// outer sequence is not walked past the result it asked for.
    /// </para>
    /// <para>
    /// The shape of the answer is the BCL's: outer order is the primary order, and an outer element
    /// that matches several inner elements produces one result per match, in inner order. An outer
    /// element with no match produces exactly one result, and the inner side of that result is
    /// <see langword="default"/>.
    /// </para>
    /// <para>
    /// An element whose key is <see langword="null"/> never matches. That is the BCL's rule too:
    /// its lookup refuses to store a null-keyed element, so a null-keyed outer element is treated
    /// as unmatched and a null-keyed inner element is simply not there.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable supplying the outer sequence.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
    /// <typeparam name="TInner">The type of the inner elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
    /// <typeparam name="TResult">The type of the projected results.</typeparam>
    public readonly struct LeftJoinValueEnumerable<TSource, TEnumerator, TOuter, TInner, TKey, TResult> : IValueEnumerable<TResult, LeftJoinValueEnumerator<TEnumerator, TOuter, TInner, TKey, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<TOuter, TEnumerator>
        where TEnumerator : struct, IEnumerator<TOuter>
    {
        private readonly TSource _outer;
        private readonly IEnumerable<TInner> _inner;
        private readonly Func<TOuter, TKey> _outerKeySelector;
        private readonly Func<TInner, TKey> _innerKeySelector;
        private readonly Func<TOuter, TInner, TResult> _resultSelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal LeftJoinValueEnumerable(TSource outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            _outer = outer;
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _outerKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
            _innerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that joins the two sequences.</summary>
        /// <returns>An enumerator positioned before the first result.</returns>
        public LeftJoinValueEnumerator<TEnumerator, TOuter, TInner, TKey, TResult> GetEnumerator()
            => new LeftJoinValueEnumerator<TEnumerator, TOuter, TInner, TKey, TResult>(_outer.GetEnumerator(), _inner, _outerKeySelector, _innerKeySelector, _resultSelector, _comparer);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // An outer element can produce any number of results - none of the outer count, inner
        // count or distinct-key count answers how many, and the results are not one contiguous
        // block.
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
    /// Streams the outer sequence past a hash lookup of the inner one, projecting one result per
    /// match and one against <see langword="default"/> when there is none.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the outer sequence.</typeparam>
    /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
    /// <typeparam name="TInner">The type of the inner elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
    /// <typeparam name="TResult">The type of the projected results.</typeparam>
    public struct LeftJoinValueEnumerator<TEnumerator, TOuter, TInner, TKey, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<TOuter>
    {
        private TEnumerator _outer;
        private readonly IEnumerable<TInner> _inner;
        private readonly Func<TOuter, TKey> _outerKeySelector;
        private readonly Func<TInner, TKey> _innerKeySelector;
        private readonly Func<TOuter, TInner, TResult> _resultSelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private JoinLookup<TKey, TInner>? _lookup;
        private List<TInner>? _matches;
        private int _matchIndex;
        private TOuter _currentOuter;
        private TResult _current;

        internal LeftJoinValueEnumerator(TEnumerator outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            _outer = outer;
            _inner = inner;
            _outerKeySelector = outerKeySelector;
            _innerKeySelector = innerKeySelector;
            _resultSelector = resultSelector;
            _comparer = comparer;
            _lookup = null;
            _matches = null;
            _matchIndex = 0;
            _currentOuter = default!;
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
            // A match list that has not been drained keeps the outer element it belongs to.
            var matches = _matches;
            if (matches != null && _matchIndex < matches.Count)
            {
                _current = _resultSelector(_currentOuter, matches[_matchIndex]);
                _matchIndex++;
                return true;
            }

            while (_outer.MoveNext())
            {
                var outer = _outer.Current;

                // The lookup is built on the first pull, and only once an outer element exists -
                // an empty outer sequence never touches the inner one.
                var lookup = _lookup;
                if (lookup is null)
                {
                    lookup = BuildLookup();
                    _lookup = lookup;
                }

                var key = _outerKeySelector(outer);
                if (!(key is null) && lookup.TryGetGroup(key, out var group))
                {
                    var groupElements = group!;
                    _currentOuter = outer;
                    _matches = groupElements;
                    _matchIndex = 1;
                    _current = _resultSelector(outer, groupElements[0]);
                    return true;
                }

                // An unmatched outer element is projected against default, not dropped.
                _current = _resultSelector(outer, default!);
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first result.</summary>
        public void Reset()
        {
            _outer.Reset();
            _lookup = null;
            _matches = null;
            _matchIndex = 0;
            _currentOuter = default!;
            _current = default!;
        }

        /// <summary>Releases the outer enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _outer.Dispose();

        private JoinLookup<TKey, TInner> BuildLookup()
        {
            var lookup = new JoinLookup<TKey, TInner>(_comparer);
            foreach (var inner in _inner)
            {
                var key = _innerKeySelector(inner);
                if (!(key is null))
                {
                    lookup.Add(key, inner);
                }
            }

            return lookup;
        }
    }
}
#endif

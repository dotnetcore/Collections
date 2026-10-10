using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>TagFirstLast</c> operator. It applies a selector to every element together with two
    /// flags that say whether the element is the first and whether it is the last of the sequence,
    /// which is how a streaming pipeline can special-case the ends of a sequence without
    /// materialising it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operator has to look one element ahead to know whether the element it is holding is the
    /// last one. That lookahead is visible from the outside: a walk of <c>n</c> elements asks the
    /// source for an element <c>n + 1</c> times, and the source is never rewound. The consequence
    /// to keep in mind is the usual one for a lookahead - the first element is handed to the
    /// selector only after the second one has been pulled.
    /// </para>
    /// <para>
    /// An empty source produces nothing; a single element is tagged as both first and last.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being tagged.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    public readonly struct TagFirstLastValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, TagFirstLastValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, bool, bool, TResult> _resultSelector;

        internal TagFirstLastValueEnumerable(TSource source, Func<T, bool, bool, TResult> resultSelector)
        {
            _source = source;
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
        }

        /// <summary>Returns a fresh enumerator that tags the first and the last element.</summary>
        /// <returns>An enumerator of tagged elements, positioned before the first one.</returns>
        public TagFirstLastValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new TagFirstLastValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _resultSelector);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The tags depend on the position within the walk, so the sequence has no length or storage
        // it could promise up front; the same holds for every operator result of the engine.
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
    /// Holds the element it is about to hand out and the result of having pulled the element after
    /// it, so the two flags can be computed once per element.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    public struct TagFirstLastValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, bool, bool, TResult> _resultSelector;
        private bool _started;
        private bool _hasNext;
        private TResult _current;

        internal TagFirstLastValueEnumerator(TEnumerator enumerator, Func<T, bool, bool, TResult> resultSelector)
        {
            _enumerator = enumerator;
            _resultSelector = resultSelector;
            _started = false;
            _hasNext = false;
            _current = default!;
        }

        /// <summary>Gets the current projected element.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // TResult is unconstrained, so the element is legitimately null-able whenever TResult is a
        // reference type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next tagged element.</summary>
        /// <returns><see langword="true"/> if an element was produced; otherwise
        /// <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                if (!_enumerator.MoveNext())
                {
                    return false;
                }

                var first = _enumerator.Current;
                _hasNext = _enumerator.MoveNext();
                _current = _resultSelector(first, true, !_hasNext);
                return true;
            }

            if (!_hasNext)
            {
                return false;
            }

            // The enumerator is parked on the element the previous call pulled ahead to. Reading it
            // before advancing is what makes it the *current* element rather than the next one.
            var element = _enumerator.Current;
            _hasNext = _enumerator.MoveNext();
            _current = _resultSelector(element, false, !_hasNext);
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first element.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _started = false;
            _hasNext = false;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}

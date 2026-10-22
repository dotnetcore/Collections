using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Split</c> operator of the P2 layer. It cuts the source wherever a separator is found
    /// and yields the segments between separators as arrays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two separator shapes are supported: a value compared with a comparer, and a predicate over
    /// the element. Both are the same operator underneath - the predicate form is what the value
    /// form degenerates to.
    /// </para>
    /// <para>
    /// Empty segments are preserved, exactly like <see cref="string.Split(char[])"/>: a leading,
    /// trailing or doubled separator produces an empty array at that position. An empty source
    /// produces no segment at all, because there is no element that could start one.
    /// </para>
    /// <para>
    /// The separator itself is never yielded. A consumer that needs it has to compare the segment
    /// boundaries itself, which is the price of keeping the segments uniformly typed.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being split.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public readonly struct SplitValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T[], SplitValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T[]>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly T _separator;
        private readonly Func<T, bool>? _separatorPredicate;
        private readonly IEqualityComparer<T> _comparer;

        internal SplitValueEnumerable(TSource source, T separator, IEqualityComparer<T>? comparer)
        {
            _source = source;
            _separator = separator;
            _separatorPredicate = null;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        internal SplitValueEnumerable(TSource source, Func<T, bool> separatorPredicate)
        {
            _source = source;
            _separator = default!;
            _separatorPredicate = separatorPredicate ?? throw new ArgumentNullException(nameof(separatorPredicate));
            _comparer = EqualityComparer<T>.Default;
        }

        /// <summary>Returns a fresh enumerator that cuts the source into segments.</summary>
        /// <returns>An enumerator positioned before the first segment.</returns>
        public SplitValueEnumerator<TEnumerator, T> GetEnumerator()
            => new SplitValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _separator, _separatorPredicate, _comparer);

        IEnumerator<T[]> IEnumerable<T[]>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The segment count depends on how many separators the source holds.
        bool IValueEnumerableHooks<T[]>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T[]>.TryGetSpan(out ReadOnlySpan<T[]> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T[]>.TryCopyTo(Span<T[]> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Collects one segment per pull. A segment ends either at a separator or at the end of the
    /// source; the flag that records whether anything has been produced yet is what makes a trailing
    /// separator yield a final empty segment instead of nothing.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public struct SplitValueEnumerator<TEnumerator, T> : IEnumerator<T[]>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly T _separator;
        private readonly Func<T, bool>? _separatorPredicate;
        private readonly IEqualityComparer<T> _comparer;
        private T[]? _current;
        private bool _producedAny;
        private bool _done;

        internal SplitValueEnumerator(TEnumerator enumerator, T separator, Func<T, bool>? separatorPredicate, IEqualityComparer<T> comparer)
        {
            _enumerator = enumerator;
            _separator = separator;
            _separatorPredicate = separatorPredicate;
            _comparer = comparer;
            _current = null;
            _producedAny = false;
            _done = false;
        }

        /// <summary>Gets the current segment.</summary>
        public T[] Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next segment.</summary>
        /// <returns><see langword="true"/> if a segment was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (_done)
            {
                return false;
            }

            var items = new List<T>();
            while (_enumerator.MoveNext())
            {
                var item = _enumerator.Current;
                if (IsSeparator(item))
                {
                    _current = items.ToArray();
                    _producedAny = true;
                    return true;
                }

                items.Add(item);
            }

            _done = true;

            // A segment that ends at the end of the source is yielded only when it holds something,
            // or when a previous segment was produced (a trailing separator leaves an empty tail).
            if (items.Count == 0 && !_producedAny)
            {
                return false;
            }

            _current = items.ToArray();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSeparator(T item)
            => _separatorPredicate is null ? _comparer.Equals(item, _separator) : _separatorPredicate(item);

        /// <summary>Sets the enumerator to its initial position, before the first segment.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _current = null;
            _producedAny = false;
            _done = false;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}

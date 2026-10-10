using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Index</c> operator. It pairs every element of the source with its zero-based
    /// position, so the positional information a caller would otherwise get from a
    /// <c>for</c> loop survives inside a pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The element type is the same tuple the BCL produces from .NET 9 onwards,
    /// <c>(int Index, T Item)</c>, down to the element names. The engine mirrors that shape rather
    /// than inventing its own so that a pipeline built here and one built on the BCL describe the
    /// same thing; a consumer can move between them without rewriting its selectors.
    /// </para>
    /// <para>
    /// The counter is incremented inside a <see langword="checked"/> block, which is what
    /// <c>System.Linq</c> does as well: a sequence longer than <see cref="int.MaxValue"/> elements
    /// fails with <see cref="OverflowException"/> instead of silently wrapping to a negative index.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being indexed.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public readonly struct IndexValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<(int Index, T Item), IndexValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<(int Index, T Item)>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;

        internal IndexValueEnumerable(TSource source)
        {
            _source = source;
        }

        /// <summary>Returns a fresh enumerator that pairs each element with its position.</summary>
        /// <returns>An enumerator of index-and-element pairs, positioned before the first pair.</returns>
        public IndexValueEnumerator<TEnumerator, T> GetEnumerator()
            => new IndexValueEnumerator<TEnumerator, T>(_source.GetEnumerator());

        IEnumerator<(int Index, T Item)> IEnumerable<(int Index, T Item)>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Indexing changes what each element is, never how many there are - but the engine keeps one
        // uniform rule for operator results (they decline the hooks) so that a generic consumer can
        // accept any operator without a special case. The source's own hooks stay reachable through
        // the source, which is what a consumer that needs a count actually holds.
        bool IValueEnumerableHooks<(int Index, T Item)>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<(int Index, T Item)>.TryGetSpan(out ReadOnlySpan<(int Index, T Item)> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<(int Index, T Item)>.TryCopyTo(Span<(int Index, T Item)> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Walks the source, counts as it goes and parks the current pair, so the counter advances once
    /// per element rather than once per read of <see cref="Current"/>.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public struct IndexValueEnumerator<TEnumerator, T> : IEnumerator<(int Index, T Item)>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private int _index;
        private (int Index, T Item) _current;

        internal IndexValueEnumerator(TEnumerator enumerator)
        {
            _enumerator = enumerator;
            _index = -1;
            _current = default;
        }

        /// <summary>Gets the current index-and-element pair.</summary>
        public (int Index, T Item) Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // The pair is a value type, so the non-generic IEnumerator.Current is the one and only
        // place it is boxed; the generic path above never is.
        object IEnumerator.Current => _current;

        /// <summary>Advances to the next pair.</summary>
        /// <returns><see langword="true"/> if a pair was produced; otherwise
        /// <see langword="false"/>.</returns>
        /// <exception cref="OverflowException">The source holds more than <see cref="int.MaxValue"/>
        /// elements.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_enumerator.MoveNext())
            {
                checked
                {
                    _index++;
                }

                _current = (_index, _enumerator.Current);
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first pair.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _index = -1;
            _current = default;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}

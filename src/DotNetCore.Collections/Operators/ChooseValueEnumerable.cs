using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Choose</c> operator: a combined filter and projection. The selector returns a
    /// <see cref="ValueTuple{TResult, Boolean}"/> whose flag says whether the element is kept;
    /// only the kept elements are produced, already projected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape mirrors F#'s <c>Seq.choose</c>: a single pass replaces the <c>Select</c> +
    /// <c>Where</c> pair that would otherwise walk the source twice (or allocate an intermediate
    /// sequence). The engine's own implementation runs on every target framework; the operator is
    /// not in the BCL.
    /// </para>
    /// </remarks>
    public readonly struct ChooseValueEnumerable<TSource, TEnumerator, T, TResult>
        : IValueEnumerable<TResult, ChooseValueEnumerator<TEnumerator, T, TResult>>,
          IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, (bool Keep, TResult Value)> _chooser;

        internal ChooseValueEnumerable(TSource source, Func<T, (bool Keep, TResult Value)> chooser)
        {
            _source = source;
            _chooser = chooser;
        }

        /// <summary>Returns a fresh enumerator that applies the chooser.</summary>
        /// <returns>An enumerator positioned before the first kept element.</returns>
        public ChooseValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new ChooseValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _chooser);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
    /// Walks the source, applies the chooser to each element, and produces only the ones whose
    /// flag is <see langword="true"/>, already projected to the result type.
    /// </summary>
    public struct ChooseValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, (bool Keep, TResult Value)> _chooser;
        private TResult _current;

        internal ChooseValueEnumerator(TEnumerator enumerator, Func<T, (bool Keep, TResult Value)> chooser)
        {
            _enumerator = enumerator;
            _chooser = chooser;
            _current = default!;
        }

        /// <summary>Gets the current projected element.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next kept element.</summary>
        /// <returns><see langword="true"/> if a kept element was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                var (keep, value) = _chooser(_enumerator.Current);
                if (keep)
                {
                    _current = value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}

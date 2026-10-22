using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Do</c> operator of the P3 layer: runs a side effect on every element as it passes and
    /// yields the element unchanged.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one operator of the layer that stays on the value-type enumerator path, because
    /// its whole purpose is to sit in the middle of a pipeline without disturbing it: a
    /// <see langword="foreach"/> over the result still binds a struct enumerator, so tapping a hot
    /// sequence costs one delegate call per element and nothing else.
    /// </para>
    /// <para>
    /// The side effect runs exactly once per element and only when the element is pulled, so a
    /// consumer that stops early sees exactly the elements it consumed and no more. Because the
    /// element itself is only forwarded, the sequence keeps its shape: the count hook is still
    /// declined here, not because the count changes (it does not) but because answering it would
    /// need the source's hooks, which the generic constraint cannot reach without boxing.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being tapped.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public readonly struct DoValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T, DoValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Action<T>? _action;
        private readonly Action<T, int>? _indexedAction;

        internal DoValueEnumerable(TSource source, Action<T> action)
        {
            _source = source;
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _indexedAction = null;
        }

        internal DoValueEnumerable(TSource source, Action<T, int> action)
        {
            _source = source;
            _action = null;
            _indexedAction = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>Returns a fresh enumerator that taps the source as it is walked.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public DoValueEnumerator<TEnumerator, T> GetEnumerator()
            => new DoValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _action, _indexedAction);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Tapping does not change the sequence, but answering the count would mean reaching the
        // source's hooks through the interface and boxing it; the walk is the cheaper answer here.
        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        // A tap has to observe every element one at a time, so a bulk copy would silently skip the
        // side effect. Declining is the only correct answer.
        bool IValueEnumerableHooks<T>.TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T>.TryCopyTo(Span<T> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Runs the tap on each element between the pull and the hand-over. The position counter only
    /// advances for the indexed form, so the plain form keeps its per-element cost at one delegate
    /// call.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public struct DoValueEnumerator<TEnumerator, T> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Action<T>? _action;
        private readonly Action<T, int>? _indexedAction;
        private int _index;

        internal DoValueEnumerator(TEnumerator enumerator, Action<T>? action, Action<T, int>? indexedAction)
        {
            _enumerator = enumerator;
            _action = action;
            _indexedAction = indexedAction;
            _index = 0;
        }

        /// <summary>Gets the element that was just tapped.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _enumerator.Current;
        }

        object IEnumerator.Current => _enumerator.Current!;

        /// <summary>Pulls the next element and runs the tap on it.</summary>
        /// <returns><see langword="true"/> if an element was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (!_enumerator.MoveNext())
            {
                return false;
            }

            if (_indexedAction is null)
            {
                _action!(_enumerator.Current);
            }
            else
            {
                _indexedAction(_enumerator.Current, _index);
                _index++;
            }

            return true;
        }

        /// <summary>Sets the enumerator to its initial position, rewinding the position counter.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _index = 0;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}

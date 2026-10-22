using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Return</c> factory of the P3 layer: a sequence of exactly one element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sequence is a value type holding the element itself, so building it allocates nothing
    /// and enumerating it allocates nothing - which is the whole reason it exists next to
    /// <c>new[] { value }</c> in a test fixture or in an early-exit branch of a pipeline.
    /// </para>
    /// <para>
    /// Because the shape is known up front, all three hooks answer: the count is one, the element
    /// is not contiguous storage (a span has to point at memory, and the element lives in the
    /// enumerable's own field), and the copy hook writes that one element.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the element.</typeparam>
    public readonly struct ReturnValueEnumerable<T> : IValueEnumerable<T, ReturnValueEnumerator<T>>, IValueEnumerableHooks<T>
    {
        private readonly T _value;

        internal ReturnValueEnumerable(T value) => _value = value;

        /// <summary>Returns a fresh enumerator over the single element.</summary>
        /// <returns>An enumerator positioned before the element.</returns>
        public ReturnValueEnumerator<T> GetEnumerator() => new ReturnValueEnumerator<T>(_value);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            count = 1;
            return true;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T>.TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T>.TryCopyTo(Span<T> destination, int offset)
        {
            if ((uint)offset > (uint)destination.Length)
            {
                return false;
            }

            if (destination.Length - offset < 1)
            {
                return false;
            }

            destination[offset] = _value;
            return true;
        }
#endif
    }

    /// <summary>
    /// Walks the single element of <see cref="ReturnValueEnumerable{T}"/>. The position field is
    /// what makes a second <see cref="MoveNext"/> answer <see langword="false"/> instead of
    /// yielding the element twice.
    /// </summary>
    /// <typeparam name="T">The type of the element.</typeparam>
    public struct ReturnValueEnumerator<T> : IEnumerator<T>
    {
        private readonly T _value;
        private int _position;

        internal ReturnValueEnumerator(T value)
        {
            _value = value;
            _position = -1;
        }

        /// <summary>Gets the element.</summary>
        /// <exception cref="InvalidOperationException">The enumerator is positioned before the
        /// element or after it.</exception>
        public T Current
        {
            get
            {
                if (_position != 0)
                {
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                }

                return _value;
            }
        }

        object IEnumerator.Current => Current!;

        /// <summary>Advances to the element, once.</summary>
        /// <returns><see langword="true"/> on the first call; <see langword="false"/>
        /// afterwards.</returns>
        public bool MoveNext()
        {
            if (_position < 0)
            {
                _position = 0;
                return true;
            }

            _position = 1;
            return false;
        }

        /// <summary>Sets the enumerator back before the element.</summary>
        public void Reset() => _position = -1;

        /// <summary>Does nothing: the sequence owns no resources.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// The <c>Throw</c> factory of the P3 layer: a sequence that fails as soon as it is walked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Building the sequence never throws; the failure is deferred to the first
    /// <c>MoveNext</c>. That is deliberate: an operator chain is assembled before it runs, and a
    /// faulted sequence is useful precisely because it can be handed to an error-handling operator
    /// (<c>Catch</c>, <c>Retry</c>, <c>OnErrorResumeNext</c>) which then observes the failure at
    /// the point it is prepared to handle it.
    /// </para>
    /// <para>
    /// The stored exception instance is rethrown as-is on every call, so a test can assert on
    /// identity. Neither hook can answer for a sequence that never yields.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements the sequence pretends to have.</typeparam>
    public readonly struct ThrowValueEnumerable<T> : IValueEnumerable<T, ThrowValueEnumerator<T>>, IValueEnumerableHooks<T>
    {
        private readonly Exception _exception;

        internal ThrowValueEnumerable(Exception exception) => _exception = exception;

        /// <summary>Returns a fresh enumerator that throws when advanced.</summary>
        /// <returns>An enumerator that never produces an element.</returns>
        public ThrowValueEnumerator<T> GetEnumerator() => new ThrowValueEnumerator<T>(_exception);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T>.TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T>.TryCopyTo(Span<T> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Fails the walk with the stored exception. Every call rethrows the same instance, which is
    /// what makes the fault reproducible for a retry.
    /// </summary>
    /// <typeparam name="T">The type of the elements the sequence pretends to have.</typeparam>
    public struct ThrowValueEnumerator<T> : IEnumerator<T>
    {
        private readonly Exception _exception;

        internal ThrowValueEnumerator(Exception exception) => _exception = exception;

        /// <summary>Gets nothing: the sequence has no element to report.</summary>
        /// <exception cref="Exception">Always: the stored exception.</exception>
        public T Current => throw _exception;

        object IEnumerator.Current => throw _exception;

        /// <summary>Fails the walk.</summary>
        /// <returns>Never returns.</returns>
        /// <exception cref="Exception">Always: the stored exception.</exception>
        public bool MoveNext() => throw _exception;

        /// <summary>Fails the rewind the same way the walk fails.</summary>
        /// <exception cref="Exception">Always: the stored exception.</exception>
        public void Reset() => throw _exception;

        /// <summary>Does nothing: the sequence owns no resources. Disposal must not throw here,
        /// because a consumer of a faulted sequence still has to be able to release it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
        }
    }
}

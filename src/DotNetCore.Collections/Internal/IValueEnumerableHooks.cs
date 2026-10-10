#if NETCOREAPP3_0_OR_GREATER
using System;
#endif

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The three-hook protocol (borrowed from ZLinq) that lets a consumer of a value enumerable
    /// take a bulk path instead of walking the sequence one element at a time. Every value
    /// enumerable the engine produces implements this interface; a consumer asks for the
    /// strongest capability the sequence can offer and falls back when the answer is
    /// <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The protocol is internal on purpose. Two of the three hooks are shaped in terms of
    /// <see cref="System.ReadOnlySpan{T}"/> / <see cref="System.Span{T}"/>, and the public API
    /// surface of this package is deliberately restricted to types that .NET Framework 4.5.1 has
    /// in the box.
    /// </para>
    /// <para>
    /// The span-shaped hooks exist only from <c>NETCOREAPP3_0_OR_GREATER</c> up. On the lower
    /// targets a <see cref="System.ReadOnlySpan{T}"/> comes from the System.Memory package and
    /// carries no JIT intrinsics, so promising a "give me a span" fast path would promise something
    /// the platform cannot deliver. This matches <c>EngineSymbols.HasIntrinsicSpan</c> (Path 3 of
    /// the compile-time gate reports <see langword="false"/> for net451 through netstandard2.1).
    /// </para>
    /// <para>
    /// <see cref="TryGetNonEnumeratedCount"/> is present on every target because it needs no span:
    /// it is the hook the paging semantics of <c>DotNetCore.Collections.Paginable</c> rests on.
    /// </para>
    /// <para>
    /// A hook must never change the observable sequence. Reporting a count or a span is a promise
    /// that the sequence really has that shape; when a sequence cannot keep the promise it answers
    /// <see langword="false"/> and the caller walks it instead.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements of the sequence.</typeparam>
    internal interface IValueEnumerableHooks<T>
    {
        /// <summary>
        /// Reports the number of elements without enumerating the sequence, if the sequence knows
        /// it up front.
        /// </summary>
        /// <param name="count">The number of elements, when the return value is
        /// <see langword="true"/>; otherwise undefined.</param>
        /// <returns><see langword="true"/> when the count is known without enumerating.</returns>
        bool TryGetNonEnumeratedCount(out int count);

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Exposes the whole sequence as one contiguous block, if it is stored that way.
        /// </summary>
        /// <param name="span">A view over the elements, when the return value is
        /// <see langword="true"/>; otherwise the default span.</param>
        /// <returns><see langword="true"/> when the sequence is contiguous.</returns>
        bool TryGetSpan(out ReadOnlySpan<T> span);

        /// <summary>
        /// Copies the whole sequence into <paramref name="destination"/> starting at
        /// <paramref name="offset"/> in a single step, if it can.
        /// </summary>
        /// <param name="destination">The buffer to copy into.</param>
        /// <param name="offset">The index in <paramref name="destination"/> to start writing at.</param>
        /// <returns><see langword="true"/> when the whole sequence was copied;
        /// <see langword="false"/> when <paramref name="destination"/> is too small or the
        /// sequence cannot bulk-copy itself, in which case nothing is written.</returns>
        bool TryCopyTo(Span<T> destination, int offset);
#endif
    }
}

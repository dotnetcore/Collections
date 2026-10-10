using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// Entry points that wrap a source in a value-type enumerable without copying it. The
    /// overload set is the four-source dispatch: an array, a <see cref="List{T}"/>, an
    /// <see cref="IReadOnlyList{T}"/> and, as the fallback, any <see cref="IEnumerable{T}"/>.
    /// The most specific overload wins, so passing a <see cref="List{T}"/> picks the list arm
    /// rather than the enumerable arm.
    /// </summary>
    /// <remarks>
    /// The wrappers are lazy and hold a reference to the source: nothing is enumerated until the
    /// result is walked.
    /// </remarks>
    public static class ValueEnumerable
    {
        /// <summary>Wraps an array in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the array.</typeparam>
        /// <param name="source">The array to enumerate.</param>
        /// <returns>A wrapper that indexes the array directly.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ArrayValueEnumerable<T> From<T>(T[] source) => new ArrayValueEnumerable<T>(source);

        /// <summary>Wraps a <see cref="List{T}"/> in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="source">The list to enumerate.</param>
        /// <returns>A wrapper that walks the list's own storage.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ListValueEnumerable<T> From<T>(List<T> source) => new ListValueEnumerable<T>(source);

        /// <summary>Wraps an <see cref="IReadOnlyList{T}"/> in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="source">The list to enumerate.</param>
        /// <returns>A wrapper that indexes the list.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ReadOnlyListValueEnumerable<T> From<T>(IReadOnlyList<T> source) => new ReadOnlyListValueEnumerable<T>(source);

        /// <summary>Wraps an arbitrary sequence in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The sequence to enumerate.</param>
        /// <returns>A wrapper that delegates to the sequence's own enumerator.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> From<T>(IEnumerable<T> source) => new EnumerableValueEnumerable<T>(source);
    }
}

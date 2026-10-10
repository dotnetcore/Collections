using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// Provides high-performance LINQ extension methods with value-type enumerators
    /// and zero-allocation hot paths.
    /// </summary>
    /// <remarks>
    /// This file holds the four entry points that wrap a source without copying it. The operator
    /// surface is the other half of the same class and lives in <c>Operators/CollectionExtensions.cs</c>,
    /// so that adding an operator never means editing the wrappers.
    /// </remarks>
    public static partial class CollectionExtensions
    {
        /// <summary>Wraps an array in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the array.</typeparam>
        /// <param name="source">The array to enumerate.</param>
        /// <returns>A wrapper that indexes the array directly.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ArrayValueEnumerable<T> ToValueEnumerable<T>(this T[] source) => new ArrayValueEnumerable<T>(source);

        /// <summary>Wraps a <see cref="List{T}"/> in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="source">The list to enumerate.</param>
        /// <returns>A wrapper that walks the list's own storage.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ListValueEnumerable<T> ToValueEnumerable<T>(this List<T> source) => new ListValueEnumerable<T>(source);

        /// <summary>Wraps an <see cref="IReadOnlyList{T}"/> in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="source">The list to enumerate.</param>
        /// <returns>A wrapper that indexes the list.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ReadOnlyListValueEnumerable<T> ToValueEnumerable<T>(this IReadOnlyList<T> source) => new ReadOnlyListValueEnumerable<T>(source);

        /// <summary>Wraps an arbitrary sequence in a value-type enumerable.</summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="source">The sequence to enumerate.</param>
        /// <returns>A wrapper that delegates to the sequence's own enumerator.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> ToValueEnumerable<T>(this IEnumerable<T> source) => new EnumerableValueEnumerable<T>(source);
    }
}

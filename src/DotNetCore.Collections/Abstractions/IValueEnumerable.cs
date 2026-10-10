using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A sequence that exposes a value-type enumerator, so that enumerating it with
    /// <see langword="foreach"/> does not allocate. Every implementation also implements
    /// <see cref="IEnumerable{T}"/>, so an instance can be handed to any API that expects a
    /// sequence and still be walked without a heap allocation when its static type is the
    /// value type.
    /// </summary>
    /// <remarks>
    /// The shape is borrowed from NetFabric.Hyperlinq: the enumerator stays a plain struct
    /// (never a <see langword="ref"/> struct) so it can be used as a type argument and can be
    /// captured by an async state machine on every target, including .NET Framework 4.5.1.
    /// </remarks>
    /// <typeparam name="T">The type of the elements of the sequence.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator over <typeparamref name="T"/>.</typeparam>
    public interface IValueEnumerable<out T, out TEnumerator> : IEnumerable<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        /// <summary>
        /// Returns a value-type enumerator over the sequence. Because the returned type is
        /// <typeparamref name="TEnumerator"/> rather than <see cref="IEnumerator{T}"/>, the
        /// enumerator is not boxed.
        /// </summary>
        /// <returns>A fresh enumerator positioned before the first element.</returns>
        new TEnumerator GetEnumerator();
    }
}

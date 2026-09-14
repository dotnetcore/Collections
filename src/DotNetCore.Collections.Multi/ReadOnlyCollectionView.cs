using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Internal live read-only wrapper over an <see cref="ICollection{T}"/>. Exposing an inner
    /// collection as <see cref="IReadOnlyCollection{T}"/> by a plain cast relies on the inner
    /// type declaring that interface — which the framework's <see cref="HashSet{T}"/> does not
    /// on net451 / net461 (neither statically nor at runtime), so the cast throws an
    /// <see cref="InvalidCastException"/> on exactly those supported targets (the hazard F6-24
    /// tracks for <see cref="MultiDictionary{TKey,TValue}"/>'s key sets). The wrapper is safe
    /// on every target and keeps the view live: enumeration and <see cref="Count"/> reflect
    /// subsequent changes to the wrapped collection.
    /// </summary>
    internal sealed class ReadOnlyCollectionView<T> : IReadOnlyCollection<T>
    {
        private readonly ICollection<T> _collection;

        public ReadOnlyCollectionView(ICollection<T> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public int Count => _collection.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// The operations a multimap property suite can generate.
    /// </summary>
    public enum MultimapOperationKind
    {
        /// <summary>Adds a value under a key.</summary>
        Add = 0,

        /// <summary>Removes one occurrence of a value under a key.</summary>
        RemoveValue = 1,

        /// <summary>Removes a key together with all of its values.</summary>
        RemoveKey = 2,

        /// <summary>Removes everything.</summary>
        Clear = 3
    }

    /// <summary>
    /// One generated multimap operation: a kind plus the key and value it targets.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public struct MultimapOperation<TKey, TValue>
    {
        /// <summary>The operation kind.</summary>
        public MultimapOperationKind Kind;

        /// <summary>The key the operation targets; ignored by <see cref="MultimapOperationKind.Clear"/>.</summary>
        public TKey Key;

        /// <summary>The value the operation targets; ignored by the key-wide forms.</summary>
        public TValue Value;
    }
}

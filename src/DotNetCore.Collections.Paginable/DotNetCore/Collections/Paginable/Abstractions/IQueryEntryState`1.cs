using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable.Abstractions {
    /// <summary>
    /// Query entry state interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueryEntryState<out T> {
        /// <summary>
        /// Gets all values
        /// </summary>
        IEnumerable<T> AllValues { get; }
    }
}
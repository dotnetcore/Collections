using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable.Abstractions
{
    public interface IQueryEntryState<T>
    {
        IEnumerable<T> AllValue { get; }
    }
}

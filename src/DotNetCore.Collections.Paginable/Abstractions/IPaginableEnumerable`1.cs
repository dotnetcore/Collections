using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public interface IPaginableEnumerable<T> : IEnumerable<T>, IPaginable<T>
    {
    }
}

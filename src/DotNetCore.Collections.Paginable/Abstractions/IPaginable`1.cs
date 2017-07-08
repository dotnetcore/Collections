using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public interface IPaginable<T> : IEnumerable<T>, IPaginable
    {
    }
}

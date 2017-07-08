using System.Linq;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public interface IPaginableQueryable<T> : IQueryable<T>, IPaginableQueryable
    {
    }
}

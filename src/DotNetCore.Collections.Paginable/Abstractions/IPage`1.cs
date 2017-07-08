using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public interface IPage<T> : IEnumerable<IPageMember<T>>, IPage
    {
        IPageMember<T> this[int index] { get; }
    }
}

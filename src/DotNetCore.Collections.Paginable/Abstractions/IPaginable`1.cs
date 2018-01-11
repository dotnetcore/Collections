using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    public interface IPaginable<T> : IEnumerable<IPage<T>>, IPaginable {
        IPage<T> GetPage(int pageNumber);
    }
}
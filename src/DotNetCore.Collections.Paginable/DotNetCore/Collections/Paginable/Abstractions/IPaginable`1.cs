using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paginable interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPaginable<T> : IEnumerable<IPage<T>>, IPaginable
    {
        /// <summary>
        /// Get page
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <returns></returns>
        IPage<T> GetPage(int pageNumber);
    }
}
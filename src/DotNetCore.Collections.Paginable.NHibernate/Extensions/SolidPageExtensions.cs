using System;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using NHibernate;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original NHibernate.QueryOver`1 result to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">NHibernate.QueryOver`1</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableNhCoreQuery<T> ToPaginable<T>(this IQueryOver<T> query, int? limitedMemberCount = null)
            => PaginableNhCoreCollFactory.CreatePageSet(query, limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original NHibernate.QueryOver`1 result to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">NHibernate.QueryOver`1</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableNhCoreQuery<T> ToPaginable<T>(this IQueryOver<T> query, int pageSize, int? limitedMemberCount = null)
            => PaginableNhCoreCollFactory.CreatePageSet(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Make original NHibernate.QueryOver`1 result to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableNhCoreQuery<T> ToPaginable<T>(this ISession session, int? limitedMemberCount = null) where T : class
            => session.QueryOver<T>().ToPaginable(limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original NHibernate.QueryOver`1 result to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableNhCoreQuery<T> ToPaginable<T>(this ISession session, int pageSize, int? limitedMemberCount = null) where T : class
            => session.QueryOver<T>().ToPaginable(pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="queryOver">original NHibernate.QueryOver`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IQueryOver<T> queryOver, int pageNumber)
            => GetPage(queryOver, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="queryOver">original NHibernate.QueryOver`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IQueryOver<T> queryOver, int pageNumber, int pageSize)
        {
            if (queryOver == null)
            {
                throw new ArgumentNullException(nameof(queryOver), $"{nameof(queryOver)} can not be null.");
            }

            if (pageNumber < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new NhCorePage<T>(queryOver, pageNumber, pageSize, NhQueryOverHelper.Count(queryOver));
        }

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISession session, int pageNumber) where T : class
            => session.QueryOver<T>().GetPage(pageNumber);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISession session, int pageNumber, int pageSize) where T : class
            => session.QueryOver<T>().GetPage(pageNumber, pageSize);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="queryOver">original NHibernate.QueryOver`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this IQueryOver<T> queryOver, int pageNumber)
            => GetPageAsync(queryOver, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="queryOver">original NHibernate.QueryOver`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static async Task<IPage<T>> GetPageAsync<T>(this IQueryOver<T> queryOver, int pageNumber, int pageSize)
        {
            if (queryOver == null)
            {
                throw new ArgumentNullException(nameof(queryOver), $"{nameof(queryOver)} can not be null.");
            }

            if (pageNumber < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new NhCorePage<T>(queryOver, pageNumber, pageSize, await NhQueryOverHelper.CountAsync(queryOver));
        }

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this ISession session, int pageNumber) where T : class
            => session.QueryOver<T>().GetPageAsync(pageNumber);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this ISession session, int pageNumber, int pageSize) where T : class
            => session.QueryOver<T>().GetPageAsync(pageNumber, pageSize);
    }
}

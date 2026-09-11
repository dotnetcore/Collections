using System;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using NHibernate;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for NHibernate
    /// </summary>
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original NHibernate.QueryOver`1 result to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">NHibernate.QueryOver`1</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static PaginableNhCoreQuery<T> ToPaginable<T>(this IQueryOver<T> query, int pageSize, int? limitedMemberCount = null)
            => PaginableNhCoreCollFactory.CreatePageSet(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Make original NHibernate.QueryOver`1 result to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static PaginableNhCoreQuery<T> ToPaginable<T>(this ISession session, int pageSize, int? limitedMemberCount = null) where T : class
            => session.QueryOver<T>().ToPaginable(pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="queryOver">original NHibernate.QueryOver`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = query.GetPage(15);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="queryOver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one, or <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static IPage<T> GetPage<T>(this IQueryOver<T> queryOver, int pageNumber, int pageSize)
        {
            if (queryOver is null)
                throw new ArgumentNullException(nameof(queryOver), $"{nameof(queryOver)} can not be null.");

            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return new NhCorePage<T>(queryOver, pageNumber, pageSize, NhQueryOverHelper.Count(queryOver));
        }

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = query.GetPage(15);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var page = query.GetPage(15, 50);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static IPage<T> GetPage<T>(this ISession session, int pageNumber, int pageSize) where T : class
            => session.QueryOver<T>().GetPage(pageNumber, pageSize);

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="queryOver">original NHibernate.QueryOver`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = await query.GetPageAsync(15, 50, cancellationToken);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="queryOver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one, or <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var page = query.GetPage(15, 50);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static async Task<IPage<T>> GetPageAsync<T>(this IQueryOver<T> queryOver, int pageNumber, int pageSize)
        {
            if (queryOver is null)
                throw new ArgumentNullException(nameof(queryOver), $"{nameof(queryOver)} can not be null.");

            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return new NhCorePage<T>(queryOver, pageNumber, pageSize, await NhQueryOverHelper.CountAsync(queryOver));
        }

        /// <summary>
        /// Get specific page from original NHibernate.QueryOver`1 source
        /// </summary>
        /// <typeparam name="T">element type of your NHibernate.QueryOver`1 source</typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = await query.GetPageAsync(15, 50, cancellationToken);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var page = query.GetPage(15, 50);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static Task<IPage<T>> GetPageAsync<T>(this ISession session, int pageNumber, int pageSize) where T : class
            => session.QueryOver<T>().GetPageAsync(pageNumber, pageSize);
    }
}
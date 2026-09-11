using System;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using SqlKata;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for sqlkata
    /// </summary>
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original SqlKata.Query result to SqlKataPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlKata.Query</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static PaginableSqlKataQuery<T> ToPaginable<T>(this Query query, int? limitedMemberCount = null)
            => PaginableSqlKataCollFactory.CreatePageSet<T>(query, limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original SqlKata.Query result to SqlKataPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlKata.Query</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static PaginableSqlKataQuery<T> ToPaginable<T>(this Query query, int pageSize, int? limitedMemberCount = null)
            => PaginableSqlKataCollFactory.CreatePageSet<T>(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original SqlKata.Query source
        /// </summary>
        /// <typeparam name="T">element type of your SqlKata.Query source</typeparam>
        /// <param name="query">original SqlKata.Query source</param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = query.GetPage(15);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static IPage<T> GetPage<T>(this Query query, int pageNumber)
            => GetPage<T>(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original SqlKata.Query source
        /// </summary>
        /// <typeparam name="T">element type of your SqlKata.Query source</typeparam>
        /// <param name="query">original SqlKata.Query source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one, or <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static IPage<T> GetPage<T>(this Query query, int pageNumber, int pageSize)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");

            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return new SqlKataPage<T>(query, pageNumber, pageSize, SqlKataHelper.Count(query));
        }

        /// <summary>
        /// Get specific page from original SqlKata.Query source
        /// </summary>
        /// <typeparam name="T">element type of your SqlKata.Query source</typeparam>
        /// <param name="query">original SqlKata.Query source</param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = await query.GetPageAsync(15, 50, cancellationToken);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static Task<IPage<T>> GetPageAsync<T>(this Query query, int pageNumber)
            => GetPageAsync<T>(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original SqlKata.Query source
        /// </summary>
        /// <typeparam name="T">element type of your SqlKata.Query source</typeparam>
        /// <param name="query">original SqlKata.Query source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one, or <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var page = query.GetPage(15, 50);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static async Task<IPage<T>> GetPageAsync<T>(this Query query, int pageNumber, int pageSize)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");

            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return new SqlKataPage<T>(query, pageNumber, pageSize, await SqlKataHelper.CountAsync(query));
        }
    }
}
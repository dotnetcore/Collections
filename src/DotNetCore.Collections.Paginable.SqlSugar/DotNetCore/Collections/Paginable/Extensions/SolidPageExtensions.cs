using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using SqlSugar;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for SqlSugar
    /// </summary>
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original SqlSugarQueryable result to SqlSugarPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlSugarQueryable</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableSqlSugarQuery<T> ToPaginable<T>(this ISugarQueryable<T> query, int? limitedMemberCount = null)
            => PaginableSqlSugarCollFactory.CreatePageSet(query, limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original SqlSugarQueryable result to SqlSugarPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlSugarQueryable</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableSqlSugarQuery<T> ToPaginable<T>(this ISugarQueryable<T> query, int pageSize, int? limitedMemberCount = null)
            => PaginableSqlSugarCollFactory.CreatePageSet(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original SqlSugarQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISugarQueryable<T> query, int pageNumber)
            => GetPage(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original SqlSugarQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISugarQueryable<T> query, int pageNumber, int pageSize)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");

            if (pageNumber < 1)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than one");

            return new SqlSugarPage<T>(query, pageNumber, pageSize, SqlSugarHelper.Count(query));
        }


        /// <summary>
        /// Get specific page from original SqlSugarQueryable source with true end-to-end async:
        /// both the total member count (<c>CountAsync</c>) and the current page members
        /// (<c>ToPageListAsync</c>) are executed as provider-native async database calls.
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this ISugarQueryable<T> query, int pageNumber, CancellationToken cancellationToken = default)
            => GetPageAsync(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original SqlSugarQueryable source with true end-to-end async:
        /// both the total member count (<c>CountAsync</c>) and the current page members
        /// (<c>ToPageListAsync</c>) are executed as provider-native async database calls.
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static async Task<IPage<T>> GetPageAsync<T>(this ISugarQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");

            if (pageNumber < 1)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than one");

            var totalMemberCount = await SqlSugarHelper.CountAsync(query, cancellationToken);

            var skip = (pageNumber - 1) * pageSize;
            if (totalMemberCount > 0 && skip >= totalMemberCount)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be greater than pages count");

            var members = await query.ToPageListAsync(pageNumber, pageSize);

            return new EnumerablePage<T>(members, pageNumber, pageSize, totalMemberCount, sourceIsFull: false);
        }

        /// <summary>
        /// Make original SqlSugarQueryable result to SqlSugarPage collection asynchronously,
        /// with the total member count obtained via provider-native <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlSugarQueryable</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<PaginableSqlSugarQuery<T>> ToPaginableAsync<T>(this ISugarQueryable<T> query, int? pageSize = null, int? limitedMemberCount = null, CancellationToken cancellationToken = default)
            => PaginableSqlSugarCollFactory.CreatePageSetAsync(query, pageSize, limitedMemberCount);
    }
}
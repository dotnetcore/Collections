using System;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using SqlKata;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Extensions for solid page for sqlkata
    /// </summary>
    public static class SolidPageExtensions {
        /// <summary>
        /// Make original SqlKata.Query result to SqlKataPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlKata.Query</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
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
        public static PaginableSqlKataQuery<T> ToPaginable<T>(this Query query, int pageSize, int? limitedMemberCount = null)
            => PaginableSqlKataCollFactory.CreatePageSet<T>(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original SqlKata.Query source
        /// </summary>
        /// <typeparam name="T">element type of your SqlKata.Query source</typeparam>
        /// <param name="query">original SqlKata.Query source</param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
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
        public static IPage<T> GetPage<T>(this Query query, int pageNumber, int pageSize) {
            if (query is null) {
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");
            }

            if (pageNumber < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new SqlKataPage<T>(query, pageNumber, pageSize, SqlKataHelper.Count(query));
        }

        /// <summary>
        /// Get specific page from original SqlKata.Query source
        /// </summary>
        /// <typeparam name="T">element type of your SqlKata.Query source</typeparam>
        /// <param name="query">original SqlKata.Query source</param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
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
        public static async Task<IPage<T>> GetPageAsync<T>(this Query query, int pageNumber, int pageSize) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");
            }

            if (pageNumber < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new SqlKataPage<T>(query, pageNumber, pageSize, await SqlKataHelper.CountAsync(query));
        }
    }
}
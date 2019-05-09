using System;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using SqlSugar;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
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
            => PaginableSqlSugarCollFactory.CreatePageSet<T>(query, limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original SqlSugarQueryable result to SqlSugarPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlSugarQueryable</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableSqlSugarQuery<T> ToPaginable<T>(this ISugarQueryable<T> query, int pageSize, int? limitedMemberCount = null)
            => PaginableSqlSugarCollFactory.CreatePageSet<T>(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original SqlSugarQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISugarQueryable<T> query, int pageNumber)
            => GetPage<T>(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

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
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");
            }

            if (pageNumber < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new SqlSugarPage<T>(query, pageNumber, pageSize, SqlSugarHelper.Count(query));
        }


        /// <summary>
        /// Get specific page from original SqlSugarQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this ISugarQueryable<T> query, int pageNumber)
            => GetPageAsync<T>(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);

        /// <summary>
        /// Get specific page from original SqlSugarQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugarQueryable source</typeparam>
        /// <param name="query">original SqlSugarQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static async Task<IPage<T>> GetPageAsync<T>(this ISugarQueryable<T> query, int pageNumber, int pageSize)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");
            }

            if (pageNumber < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new SqlSugarPage<T>(query, pageNumber, pageSize, (await SqlSugarHelper.CountAsync(query)));
        }
    }
}

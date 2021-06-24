using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using FreeSql;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for FreeSql
    /// </summary>
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original FreeSql.Select`1 result to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="select">FreeSql.Select`1</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> ToPaginable<T>(this ISelect<T> select, int? limitedMemberCount = null, bool? includeNestedMembers = null) where T : class
            => PaginableFreeSqlCollFactory.CreatePageSet(select, limitedMemberCount: limitedMemberCount, includeNestedMembers: includeNestedMembers);

        /// <summary>
        /// Make original FreeSql.Select`1 result to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="select">FreeSql.Select`1</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> ToPaginable<T>(this ISelect<T> select, int pageSize, int? limitedMemberCount = null, bool? includeNestedMembers = null)
            where T : class
            => PaginableFreeSqlCollFactory.CreatePageSet(select, pageSize, limitedMemberCount, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISelect<T> select, int pageNumber, bool includeNestedMembers = false) where T : class
            => GetPage(select, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this ISelect<T> select, int pageNumber, int pageSize, bool includeNestedMembers = false) where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select), $"{nameof(select)} can not be null.");

            if (pageNumber < 0)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");

            if (pageSize < 0)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");

            return new FreeSqlPage<T>(select, pageNumber, pageSize, FreeSqlHelper.Count(select).AsInt32(), includeNestedMembers);
        }

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this ISelect<T> select, int pageNumber, bool includeNestedMembers = false) where T : class
            => GetPageAsync(select, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static async Task<IPage<T>> GetPageAsync<T>(this ISelect<T> select, int pageNumber, int pageSize, bool includeNestedMembers = false) where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select), $"{nameof(select)} can not be null.");
            
            if (pageNumber < 0)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            
            if (pageSize < 0)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            
            return new FreeSqlPage<T>(select, pageNumber, pageSize, (await FreeSqlHelper.CountAsync(select)).AsInt32(), includeNestedMembers);
        }
    }
}
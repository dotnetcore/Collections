using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> ToPaginable<T>(this DbSet<T> source, int? limitedMemberCount = null, bool? includeNestedMembers = null) where T : class
            => source.Select.ToPaginable(limitedMemberCount: limitedMemberCount, includeNestedMembers);

        /// <summary>
        /// Make original FreeSql.Select`1 result to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> ToPaginable<T>(this DbSet<T> source, int pageSize, int? limitedMemberCount = null, bool? includeNestedMembers = null) where T : class
            => source.Select.ToPaginable(pageSize, limitedMemberCount, includeNestedMembers);

        /// <summary>
        /// Make original FreeSql.Select`1 result to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> ToPaginable<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int? limitedMemberCount = null,
            bool? includeNestedMembers = null) where T : class
            => source.Where(predicate).ToPaginable(limitedMemberCount: limitedMemberCount, includeNestedMembers);

        /// <summary>
        /// Make original FreeSql.Select`1 result to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        public static PaginableFreeSqlQuery<T> ToPaginable<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageSize, int? limitedMemberCount = null,
            bool? includeNestedMembers = null) where T : class
            => source.Where(predicate).ToPaginable(pageSize, limitedMemberCount, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, int pageNumber, bool includeNestedMembers = false) where T : class
            => source.Select.GetPage(pageNumber, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, int pageNumber, int pageSize, bool includeNestedMembers = false) where T : class
            => source.Select.GetPage(pageNumber, pageSize, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, bool includeNestedMembers = false) where T : class
            => source.Where(predicate).GetPage(pageNumber, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, int pageSize, bool includeNestedMembers = false)
            where T : class
            => source.Where(predicate).GetPage(pageNumber, pageSize, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, int pageNumber, bool includeNestedMembers = false) where T : class
            => source.Select.GetPageAsync(pageNumber, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, int pageNumber, int pageSize, bool includeNestedMembers = false) where T : class
            => source.Select.GetPageAsync(pageNumber, pageSize, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, bool includeNestedMembers = false) where T : class
            => source.Where(predicate).GetPageAsync(pageNumber, includeNestedMembers);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="source">FreeSql DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, int pageSize, bool includeNestedMembers = false)
            where T : class
            => source.Where(predicate).GetPageAsync(pageNumber, pageSize, includeNestedMembers);
    }
}
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for EntityFramework
    /// </summary>
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original query result to QueryablePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableQueryable<T> ToPaginable<T>(this DbSet<T> source, int? limitedMemberCount = null) where T : class
            => source.AsQueryable().ToPaginable(limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original query result to QueryablePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableQueryable<T> ToPaginable<T>(this DbSet<T> source, int pageSize, int? limitedMemberCount = null) where T : class
            => source.AsQueryable().ToPaginable(pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, int pageNumber) where T : class
            => source.AsQueryable().GetPage(pageNumber);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, int pageNumber, int pageSize) where T : class
            => source.AsQueryable().GetPage(pageNumber, pageSize);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber) where T : class
            => source.Where(predicate).GetPage(pageNumber);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, int pageSize) where T : class
            => source.Where(predicate).GetPage(pageNumber, pageSize);
    }
}
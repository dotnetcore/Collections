using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for EntityFrameworkCore
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
        /// Make original queryable source to QueryablePage collection asynchronously,
        /// with the total member count obtained via provider-native <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable source</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static async Task<PaginableQueryable<T>> ToPaginableAsync<T>(this IQueryable<T> queryable, int? pageSize = null, int? limitedMemberCount = null, CancellationToken cancellationToken = default)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable), $"{nameof(queryable)} can not be null.");

            var size = pageSize ?? PaginableSettingsManager.Settings.DefaultPageSize;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, await queryable.CountAsync(cancellationToken));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount > 0 && limitedMemberCount.HasValue
                ? new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount);
        }

        /// <summary>
        /// Make original query result to QueryablePage collection asynchronously,
        /// with the total member count obtained via provider-native <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<PaginableQueryable<T>> ToPaginableAsync<T>(this DbSet<T> source, int? pageSize = null, int? limitedMemberCount = null, CancellationToken cancellationToken = default) where T : class
            => source.AsQueryable().ToPaginableAsync(pageSize, limitedMemberCount, cancellationToken);

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

        /// <summary>
        /// Get specific page from original queryable source with true end-to-end async:
        /// both the total member count (<c>CountAsync</c>) and the current page members
        /// (<c>ToListAsync</c>) are executed as provider-native async database calls.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this IQueryable<T> queryable, int pageNumber, CancellationToken cancellationToken = default)
            => queryable.GetPageAsync(pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, cancellationToken);

        /// <summary>
        /// Get specific page from original queryable source with true end-to-end async:
        /// both the total member count (<c>CountAsync</c>) and the current page members
        /// (<c>ToListAsync</c>) are executed as provider-native async database calls.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static async Task<IPage<T>> GetPageAsync<T>(this IQueryable<T> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable), $"{nameof(queryable)} can not be null.");

            if (pageNumber < 1)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than one");

            var totalMemberCount = await queryable.CountAsync(cancellationToken);

            var skip = (pageNumber - 1) * pageSize;
            if (totalMemberCount > 0 && skip >= totalMemberCount)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be greater than pages count");

            var members = await queryable.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

            return new EnumerablePage<T>(members, pageNumber, pageSize, totalMemberCount, sourceIsFull: false);
        }

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source with true end-to-end async.
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, int pageNumber, CancellationToken cancellationToken = default) where T : class
            => source.AsQueryable().GetPageAsync(pageNumber, cancellationToken);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source with true end-to-end async.
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default) where T : class
            => source.AsQueryable().GetPageAsync(pageNumber, pageSize, cancellationToken);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source with true end-to-end async.
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, CancellationToken cancellationToken = default) where T : class
            => source.Where(predicate).GetPageAsync(pageNumber, cancellationToken);

        /// <summary>
        /// Get specific page from original EfCore DbSet`1 source with true end-to-end async.
        /// </summary>
        /// <typeparam name="T">element type of your EfCore DbSet`1 source</typeparam>
        /// <param name="source">DbSet source</param>
        /// <param name="predicate">Predicate</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this DbSet<T> source, Expression<Func<T, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default) where T : class
            => source.Where(predicate).GetPageAsync(pageNumber, pageSize, cancellationToken);
    }
}

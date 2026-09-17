using System;
using System.Linq.Expressions;
using System.Threading;
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
        /// <example>
        /// <code>
        /// var paginable = freeSql.Select&lt;ExampleModel&gt;().ToPaginable();
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var paginable = query.ToPaginable(50);
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// var page = query.GetPage(15);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
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
        /// <exception cref="ArgumentNullException"><paramref name="select"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one, or <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var page = freeSql.Select&lt;ExampleModel&gt;().GetPage(15, 50);
        /// </code>
        /// </example>
        public static IPage<T> GetPage<T>(this ISelect<T> select, int pageNumber, int pageSize, bool includeNestedMembers = false) where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select), $"{nameof(select)} can not be null.");

            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return new FreeSqlPage<T>(select, pageNumber, pageSize, FreeSqlHelper.Count(select).AsInt32(), includeNestedMembers);
        }

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source with true end-to-end async:
        /// both the total member count (<c>CountAsync</c>) and the current page members
        /// (<c>ToListAsync</c>) are executed as provider-native async database calls.
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = await query.GetPageAsync(15, 50, cancellationToken);
        /// var totalMemberCount = page.TotalMemberCount;
        /// </code>
        /// </example>
        public static Task<IPage<T>> GetPageAsync<T>(this ISelect<T> select, int pageNumber, bool includeNestedMembers = false, CancellationToken cancellationToken = default) where T : class
            => GetPageAsync(select, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, includeNestedMembers, cancellationToken);

        /// <summary>
        /// Get specific page from original FreeSql.Select`1 source with true end-to-end async:
        /// both the total member count (<c>CountAsync</c>) and the current page members
        /// (<c>ToListAsync</c>) are executed as provider-native async database calls.
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="select"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one, <paramref name="pageSize"/> is less than one, or
        /// <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <example>
        /// <code>
        /// var page = await freeSql.Select&lt;ExampleModel&gt;().GetPageAsync(15, 50, cancellationToken);
        /// </code>
        /// </example>
        public static async Task<IPage<T>> GetPageAsync<T>(this ISelect<T> select, int pageNumber, int pageSize, bool includeNestedMembers = false, CancellationToken cancellationToken = default) where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select), $"{nameof(select)} can not be null.");

            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            var totalMemberCount = (await FreeSqlHelper.CountAsync(select, cancellationToken)).AsInt32();

            var skip = (pageNumber - 1) * pageSize;
            if (totalMemberCount > 0 && skip >= totalMemberCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be greater than pages count");

            var members = await select.Page(pageNumber, pageSize).ToListAsync(includeNestedMembers, cancellationToken);

            return new EnumerablePage<T>(members, pageNumber, pageSize, totalMemberCount, sourceIsFull: false);
        }

        /// <summary>
        /// Make original FreeSql.Select`1 result to FreeSqlPage collection asynchronously,
        /// with the total member count obtained via provider-native <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="select">FreeSql.Select`1</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var paginable = await freeSql.Select&lt;ExampleModel&gt;().ToPaginableAsync(50, cancellationToken: cancellationToken);
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public static Task<PaginableFreeSqlQuery<T>> ToPaginableAsync<T>(this ISelect<T> select, int? pageSize = null, int? limitedMemberCount = null, bool? includeNestedMembers = null, CancellationToken cancellationToken = default) where T : class
            => PaginableFreeSqlCollFactory.CreatePageSetAsync(select, pageSize, limitedMemberCount, includeNestedMembers, cancellationToken);
    }
}
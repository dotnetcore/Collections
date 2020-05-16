using System;
using System.Threading.Tasks;
using Chloe;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Extensions for solid page for Chloe
    /// </summary>
    public static class SolidPageExtensions {
        /// <summary>
        /// Make original ChloeQueryable result to ChloePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">ChloeQueryable</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableChloeQuery<T> ToPaginable<T>(this IQuery<T> query, int? limitedMemberCount = null)
            => PaginableChloeCollFactory.CreatePageSet<T>(query, limitedMemberCount: limitedMemberCount);

        /// <summary>
        /// Make original ChloeQueryable result to ChloePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">ChloeQueryable</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableChloeQuery<T> ToPaginable<T>(this IQuery<T> query, int pageSize, int? limitedMemberCount = null)
            => PaginableChloeCollFactory.CreatePageSet<T>(query, pageSize, limitedMemberCount);

        /// <summary>
        /// Get specific page from original ChloeQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your ChloeQueryable source</typeparam>
        /// <param name="query">original ChloeQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IQuery<T> query, int pageNumber, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null)
            => GetPage<T>(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, additionalQueryFunc);

        /// <summary>
        /// Get specific page from original ChloeQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your ChloeQueryable source</typeparam>
        /// <param name="query">original ChloeQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IQuery<T> query, int pageNumber, int pageSize, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null) {
            if (query is null) {
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");
            }

            if (pageNumber < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new ChloePage<T>(query, pageNumber, pageSize, ChloeHelper.Count(query), additionalQueryFunc);
        }
    }
}
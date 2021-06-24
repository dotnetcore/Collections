using System;
using Dos.ORM;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for solid page for DosORM
    /// </summary>
    public static class SolidPageExtensions
    {
        /// <summary>
        /// Make original DosQueryable result to DosPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">DosQueryable</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static PaginableDosQuery<T> ToPaginable<T>(this FromSection<T> query, int? limitedMemberCount = null,
            Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) where T : Entity
            => PaginableDosCollFactory.CreatePageSet(query, limitedMemberCount: limitedMemberCount, additionalQueryFunc: additionalQueryFunc);

        /// <summary>
        /// Make original DosQueryable result to DosPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">DosQueryable</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static PaginableDosQuery<T> ToPaginable<T>(this FromSection<T> query, int pageSize, int? limitedMemberCount = null,
            Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) where T : Entity
            => PaginableDosCollFactory.CreatePageSet(query, pageSize, limitedMemberCount, additionalQueryFunc: additionalQueryFunc);

        /// <summary>
        /// Get specific page from original DosQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your DosQueryable source</typeparam>
        /// <param name="query">original DosQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this FromSection<T> query, int pageNumber, Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) where T : Entity
            => GetPage(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, additionalQueryFunc: additionalQueryFunc);

        /// <summary>
        /// Get specific page from original DosQueryable source
        /// </summary>
        /// <typeparam name="T">element type of your DosQueryable source</typeparam>
        /// <param name="query">original DosQueryable source</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this FromSection<T> query, int pageNumber, int pageSize, Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null)
            where T : Entity
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");

            if (pageNumber < 0)
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");

            if (pageSize < 0)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");

            return new DosPage<T>(query, pageNumber, pageSize, DosHelper.Count(query), additionalQueryFunc: additionalQueryFunc);
        }
    }
}
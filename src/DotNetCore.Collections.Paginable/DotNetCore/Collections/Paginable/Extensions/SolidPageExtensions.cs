﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Extensions for solid page
    /// </summary>
    public static class SolidPageExtensions {
        /// <summary>
        /// Make original enumerable result to EnumerablePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="enumerable">original enumerable result</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableEnumerable<T> ToPaginable<T>(this IEnumerable<T> enumerable, int? limitedMemberCount = null) {
            return PaginableCollectionFactory.CreatePageSet(enumerable, limitedMemberCount: limitedMemberCount);
        }

        /// <summary>
        /// Make original enumerable result to EnumerablePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="enumerable">original enumerable result</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableEnumerable<T> ToPaginable<T>(this IEnumerable<T> enumerable, int pageSize, int? limitedMemberCount = null) {
            return PaginableCollectionFactory.CreatePageSet(enumerable, pageSize, limitedMemberCount);
        }

        /// <summary>
        /// Get specific page from original enumerable result
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="enumerable">original enumerable result</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IEnumerable<T> enumerable, int pageNumber) {
            return GetPage(enumerable, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);
        }

        /// <summary>
        /// Get specific page from original enumerable result
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="enumerable">original enumerable result</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IEnumerable<T> enumerable, int pageNumber, int pageSize) {
            if (enumerable is null) {
                throw new ArgumentNullException(nameof(enumerable), $"{nameof(enumerable)} can not be null.");
            }

            if (pageNumber < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new EnumerablePage<T>(enumerable, pageNumber, pageSize, enumerable.Count());
        }

        /// <summary>
        /// Make original queryable source to QueryablePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable result</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableQueryable<T> ToPaginable<T>(this IQueryable<T> queryable, int? limitedMemberCount = null) {
            return PaginableCollectionFactory.CreatePageSet(queryable, limitedMemberCount);
        }

        /// <summary>
        /// Make original queryable source to QueryablePage collection.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable result</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableQueryable<T> ToPaginable<T>(this IQueryable<T> queryable, int pageSize, int? limitedMemberCount = null) {
            return PaginableCollectionFactory.CreatePageSet(queryable, pageSize, limitedMemberCount);
        }

        /// <summary>
        /// Get specific page from original queryable source
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable result</param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IQueryable<T> queryable, int pageNumber) {
            return GetPage(queryable, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);
        }

        /// <summary>
        /// Get specific page from original queryable source
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable result</param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IPage<T> GetPage<T>(this IQueryable<T> queryable, int pageNumber, int pageSize) {
            if (queryable is null) {
                throw new ArgumentNullException(nameof(queryable), $"{nameof(queryable)} can not be null.");
            }

            if (pageNumber < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new QueryablePage<T>(queryable, pageNumber, pageSize, queryable.Count());
        }

        /// <summary>
        /// Get specific page from original queryable source
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryableTask"></param>
        /// <param name="pageNumber">page number</param>
        /// <returns></returns>
        public static Task<IPage<T>> GetPageAsync<T>(this Task<IQueryable<T>> queryableTask, int pageNumber) {
            return GetPageAsync(queryableTask, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);
        }

        /// <summary>
        /// Get specific page from original queryable source
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryableTask"></param>
        /// <param name="pageNumber">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static async Task<IPage<T>> GetPageAsync<T>(this Task<IQueryable<T>> queryableTask, int pageNumber, int pageSize) {
            if (queryableTask is null) {
                throw new ArgumentNullException(nameof(queryableTask), $"{nameof(queryableTask)} can not be null.");
            }

            if (pageNumber < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            var queryable = await queryableTask;

            return new QueryablePage<T>(queryable, pageNumber, pageSize, queryable.Count());
        }
    }
}
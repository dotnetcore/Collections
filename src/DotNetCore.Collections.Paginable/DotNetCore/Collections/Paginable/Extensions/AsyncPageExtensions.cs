#if !NET451
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions that stream a paged source as an asynchronous sequence of pages
    /// (<see cref="IAsyncEnumerable{T}"/> of <see cref="IPage{T}"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions exist on every target framework <b>except</b> <c>net451</c>: on that
    /// generation there is no reachable <see cref="IAsyncEnumerable{T}"/> at all
    /// (<c>Microsoft.Bcl.AsyncInterfaces</c> ships <c>net461</c> / <c>netstandard2.0</c> /
    /// <c>netstandard2.1</c> assets only, and <c>net451</c> caps at <c>netstandard1.2</c>), so the
    /// whole face is compiled out there. On <c>net461</c> / <c>net47</c> / <c>net48</c> /
    /// <c>netstandard2.0</c> the polyfill package is a dependency of this package; from
    /// <c>netstandard2.1</c> and <c>.NET 6</c> upwards the type is in-box and no extra dependency
    /// is added.
    /// </para>
    /// <para>
    /// The stream is lazy: page <c>N</c> is materialised when the consumer moves to it, and
    /// nothing is prefetched, so breaking out of the loop stops the walk where it stands. The
    /// pages themselves come from the same lazy, cached page sets the synchronous API uses, so a
    /// page read once during a stream is the same page read again afterwards.
    /// </para>
    /// <para>
    /// Cancellation is observed <b>between</b> pages: an already cancelled token stops the stream
    /// before the next page is fetched, the move that would have fetched it raises
    /// <see cref="OperationCanceledException"/>, and a page that is being materialised is not
    /// interrupted.
    /// </para>
    /// <para>
    /// This is an asynchronous <b>shape</b> over the page set's own page fetch, which is
    /// synchronous: pages arrive through <see cref="IPaginable{T}.GetPage"/> by way of the page
    /// set's enumerator, so a provider-backed page set still runs each provider call on the
    /// calling thread. What the stream adds is that no page is fetched until it is asked for, that
    /// the walk stops where the consumer stops, and that a cancelled token stops it before the
    /// next provider call. For a page fetch that is itself non-blocking, use the provider's own
    /// asynchronous entry: EF Core, FreeSql and SqlSugar accept a <see cref="CancellationToken"/>
    /// there, FreeSql.DbContext, NHibernate and SqlKata do not, and Chloe, DosOrm and
    /// EntityFramework 6 have no asynchronous entry at all.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await foreach (var page in list.ToPagesAsync(50, cancellationToken))
    /// {
    ///     foreach (var member in page)
    ///     {
    ///         Console.WriteLine(member.Value);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class AsyncPageExtensions
    {
        /// <summary>
        /// Stream the pages of an existing page set as an asynchronous sequence of pages.
        /// </summary>
        /// <typeparam name="T">element type of your page members</typeparam>
        /// <param name="paginable">page set to stream</param>
        /// <param name="cancellationToken">observed between pages</param>
        /// <returns>a lazy, cancellable sequence of pages</returns>
        /// <exception cref="ArgumentNullException"><paramref name="paginable"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var paginable = list.ToPaginable(50);
        /// await foreach (var page in paginable.ToPagesAsync(cancellationToken))
        /// {
        ///     // one page at a time, nothing prefetched
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this IPaginable<T> paginable, CancellationToken cancellationToken = default)
        {
            if (paginable is null)
                throw new ArgumentNullException(nameof(paginable), $"{nameof(paginable)} can not be null.");

            return Stream(paginable, cancellationToken);
        }

        /// <summary>
        /// Stream the pages of an original enumerable result, using the configured default page size.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">original enumerable result</param>
        /// <param name="cancellationToken">observed between pages</param>
        /// <returns>a lazy, cancellable sequence of pages</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// await foreach (var page in list.ToPagesAsync(cancellationToken))
        /// {
        ///     // pages of PaginableSettingsManager.Settings.DefaultPageSize members
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken = default)
            => ToPagesAsync(source, PaginableSettingsManager.Settings.DefaultPageSize, cancellationToken);

        /// <summary>
        /// Stream the pages of an original enumerable result.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="source">original enumerable result</param>
        /// <param name="pageSize">page size (must be greater than or equal to one)</param>
        /// <param name="cancellationToken">observed between pages</param>
        /// <returns>a lazy, cancellable sequence of pages</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than one.</exception>
        /// <remarks>
        /// Argument validation is synchronous, exactly like the <c>GetPage</c> family: the
        /// exceptions leave the call itself, not a faulted task and not the first
        /// <c>MoveNextAsync</c>.
        /// </remarks>
        /// <example>
        /// <code>
        /// await foreach (var page in list.ToPagesAsync(50, cancellationToken))
        /// {
        ///     Console.WriteLine(page.CurrentPageNumber);
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this IEnumerable<T> source, int pageSize, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source), $"{nameof(source)} can not be null.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            // Reuse the synchronous page set: its enumerator is already lazy and cached, so the
            // stream adds the asynchronous shape without a second paging implementation.
            return ToPagesAsync(source.ToPaginable(pageSize), cancellationToken);
        }

        // The cancellation check sits BEFORE the page is pulled, not after: an already cancelled
        // token must stop the stream without materialising a single page (the pull is what runs
        // the query), and cancelling between pages must not fetch the next one either. Written as
        // an explicit loop rather than `foreach` so the check and the pull cannot be reordered by
        // accident; the finally block is what `await foreach` + break relies on to release the
        // underlying enumerator.
        private static async IAsyncEnumerable<IPage<T>> Stream<T>(IPaginable<T> paginable, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var enumerator = paginable.GetEnumerator();
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!enumerator.MoveNext())
                        break;

                    yield return enumerator.Current;
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }
    }
}
#endif

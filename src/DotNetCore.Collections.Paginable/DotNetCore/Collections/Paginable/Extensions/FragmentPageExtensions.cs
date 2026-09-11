using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for building a page out of an already-sliced fragment.
    /// </summary>
    /// <remarks>
    /// These are the sugar over <see cref="Paginable.CreatePage{T}(System.Collections.Generic.IEnumerable{T}, int, int, int)"/>.
    /// The name is <c>ToPage</c>, not <c>GetPage</c>, on purpose: <c>GetPage</c> means
    /// "slice this page out of a full source", whereas <c>ToPage</c> means "this sequence
    /// already is one page".
    /// </remarks>
    public static class FragmentPageExtensions
    {
        /// <summary>
        /// Treats this sequence as the exact content of one page and wraps it in an
        /// <see cref="IPage{T}"/>. The fragment is <b>not</b> re-sliced.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one; <paramref name="pageSize"/> is less than
        /// one; <paramref name="totalMemberCount"/> is negative or exceeds the configured
        /// <c>MaxMemberItems</c>; or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than <paramref name="pageSize"/>, or more
        /// than the metadata says the page holds.
        /// </exception>
        /// <example>
        /// <code>
        /// // items already holds one page - do NOT call GetPage on it
        /// IPage&lt;Order&gt; page = items.ToPage(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// </code>
        /// </example>
        public static IPage<T> ToPage<T>(this IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            return Paginable.CreatePage(fragment, pageNumber, pageSize, totalMemberCount);
        }

        /// <summary>
        /// Treats this sequence as the exact content of one page and wraps it in an
        /// <see cref="IPage{T}"/>, selecting the fragment checking strictness. The fragment is
        /// <b>not</b> re-sliced.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <param name="options"><see cref="PageCreationOptions.Lenient"/> (the 6.1 behaviour) or
        /// <see cref="PageCreationOptions.Strict"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one; <paramref name="pageSize"/> is less than
        /// one; <paramref name="totalMemberCount"/> is negative or exceeds the configured
        /// <c>MaxMemberItems</c>; or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than <paramref name="pageSize"/> or more
        /// than the metadata says the page holds; in <see cref="PageCreationOptions.Strict"/> mode
        /// also when it carries fewer than the metadata says.
        /// </exception>
        /// <example>
        /// <code>
        /// IPage&lt;Order&gt; page = items.ToPage(pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Strict);
        /// </code>
        /// </example>
        public static IPage<T> ToPage<T>(this IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount, PageCreationOptions options)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            return Paginable.CreatePage(fragment, pageNumber, pageSize, totalMemberCount, options);
        }
    }
}

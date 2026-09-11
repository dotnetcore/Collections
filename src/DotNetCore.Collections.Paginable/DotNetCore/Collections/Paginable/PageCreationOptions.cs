using System;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Selects how strictly <see cref="Paginable.CreatePage{T}(System.Collections.Generic.IEnumerable{T},PageFragmentInfo,PageCreationOptions)"/>
    /// checks a fragment against its paging metadata. 6.1's behaviour is the lenient one and stays
    /// the default; strict mode is opt-in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A fragment <b>shorter</b> than the metadata says the page holds is tolerated by default: an
    /// upstream row can genuinely disappear between the <c>COUNT(*)</c> and the fetch (a concurrent
    /// delete), and 6.1 decided the page must stay buildable with the metadata value reported by
    /// <see cref="IPage.CurrentPageSize"/>. That tolerance can also hide a bug - a stale
    /// <c>totalMemberCount</c>, a wrongly sliced fragment, a query that silently returned fewer
    /// rows - so strict mode turns the same situation into an <see cref="ArgumentException"/>.
    /// </para>
    /// <para>
    /// The checks that existed before - a fragment longer than the page size, and a fragment
    /// carrying more members than the metadata allows - throw in both modes; they are
    /// data-corruption signals, not tolerance questions.
    /// </para>
    /// </remarks>
    public sealed class PageCreationOptions
    {
        /// <summary>
        /// The 6.1 behaviour: a fragment shorter than the metadata expects is tolerated and
        /// <see cref="IPage.CurrentPageSize"/> keeps reporting the metadata value. This is what the
        /// overloads without an options parameter use.
        /// </summary>
        public static PageCreationOptions Lenient { get; } = new PageCreationOptions(false);

        /// <summary>
        /// Strict checking: a fragment shorter than the metadata expects throws
        /// <see cref="ArgumentException"/> naming the fragment.
        /// </summary>
        public static PageCreationOptions Strict { get; } = new PageCreationOptions(true);

        /// <summary>
        /// Gets a value indicating whether short-fragment checking is enabled.
        /// </summary>
        public bool IsStrict { get; }

        private PageCreationOptions(bool isStrict)
        {
            IsStrict = isStrict;
        }
    }
}

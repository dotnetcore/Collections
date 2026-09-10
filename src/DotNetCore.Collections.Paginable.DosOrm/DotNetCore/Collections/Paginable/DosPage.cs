using System;
using System.Collections.Generic;
using Dos.ORM;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Dos.ORM page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DosPage<T> : PageBase<T> where T : Entity
    {
        /// <summary>
        /// Dos.ORM page
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="additionalQueryFunc"></param>
        // ReSharper disable once RedundantBaseConstructorCall
        public DosPage(FromSection<T> query, int currentPageNumber, int pageSize, int totalMemberCount, Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) : base(false)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new DosQueryState<T>(query, currentPageNumber, pageSize, additionalQueryFunc);
            InitializeMetaInfo(currentPageNumber, pageSize, totalMemberCount, skip);
            base._initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);
        }

        /// <summary>
        /// Get empty page
        /// </summary>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = EmptyPage&lt;ExampleModel&gt;.Empty();
        /// </code>
        /// </example>
        public static EmptyPage<T> Empty() => new();

        private Func<DosQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
        {
            // s = page size
            // k = skip
            base._memberList = new List<IPageMember<T>>(s);
            for (var i = 0; i < s; i++)
            {
                base._memberList.Add(PageMemberFactory.Create<T>(state, i, ref k));
            }
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable RedundantCast
// ReSharper disable RedundantBaseQualifier

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Enumerable page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumerablePage<T> : PageBase<T>
    {
        /// <summary>
        /// Enumerable page
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="sourceIsFull"></param>
        public EnumerablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMemberCount, bool sourceIsFull = true) : base(sourceIsFull)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            InitializeMetaInfo(currentPageNumber, pageSize, totalMemberCount, skip);
            base._initializeAction = InitializeMemberList()(enumerable)(CurrentPageSize)(skip);
        }

        /// <summary>
        /// Get empty page
        /// </summary>
        /// <returns></returns>
        public static EmptyPage<T> Empty() => new();

        private Func<IEnumerable<T>, Func<int, Func<int, Action>>> InitializeMemberList()
            => array => s => k => () =>
            {
                // s = current page size
                // k = skip
                base._memberList = new List<IPageMember<T>>(s);
                if (array is IQueryable<T> query)
                {
                    var realQuery = query.Skip(k).Take(s).ToList();
                    var offset = 0;
                    foreach (var item in realQuery)
                    {
                        base._memberList.Add(new PageMember<T>(item, offset++, ref k));
                    }
                }
                else if (base.SourceIsFull)
                {
                    // Materialize the current page with a single Skip/Take enumeration.
                    // ElementAt(k + i) per member re-enumerates from the start for non-IList
                    // sources, costing O(pageSize * skip) per page (grows with page number).
                    var realMembers = array.Skip(k).Take(s).ToList();
                    var offset = 0;
                    foreach (var item in realMembers)
                    {
                        base._memberList.Add(new PageMember<T>(item, offset++, ref k));
                    }
                }
                else
                {
                    var realMembers = array.Take(s).ToList();
                    var offset = 0;
                    foreach (var item in realMembers)
                    {
                        base._memberList.Add(new PageMember<T>(item, offset++, ref k));
                    }
                }
            };
    }
}
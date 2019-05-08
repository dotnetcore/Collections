using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetCore.Collections.Paginable.Internal;
using SqlKata;
using SqlKata.Execution;

namespace DotNetCore.Collections.Paginable
{
    public class SqlKataPage<T> : PageBase<T>
    {

        public SqlKataPage(Query query, int currentPageNumber, int pageSize, int totalMemberCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new SqlKataQueryState<T>(query, currentPageNumber, pageSize);
            InitializeMetaInfo()(currentPageNumber)(pageSize)(totalMemberCount)(skip)();
            base._initializeAction = InitializeMemberList()(state)(pageSize)(skip);
        }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        private Func<int, Func<int, Func<int, Func<int, Action>>>> InitializeMetaInfo() => c => s => t => k => () =>
             {
                 // c = current page number
                 // s = page size
                 // t = total member count
                 // k = skip
                 var totalPageCount = (int)Math.Ceiling((double)t / (double)s);
                 totalPageCount = totalPageCount < 0 ? 0 : totalPageCount;
                 base.TotalPageCount = totalPageCount == 0 ? 1 : totalPageCount;
                 base.TotalMemberCount = t;
                 base.CurrentPageNumber = c;
                 base.PageSize = s;
                 base.CurrentPageSize = c == totalPageCount
                     ? k == 0
                         ? t
                         : t % k
                     : totalPageCount == 0
                         ? 0
                         : s;

                 base.HasPrevious = c > 1;
                 base.HasNext = c < base.TotalPageCount;
             };

        private Func<SqlKataQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public class EnumerablePage<T> : PageBase<T>
    {
        // ReSharper disable once InconsistentNaming
        private readonly T[] m_pinedEnumerable;

        public EnumerablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMemberCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;

            var list = new T[pageSize];
            Array.Copy(enumerable.ToArray(), skip, list, 0, pageSize);
            m_pinedEnumerable = list;

            base.TotalPageCount = (int)Math.Ceiling((double)totalMemberCount / (double)pageSize);
            base.TotalMemberCount = totalMemberCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPageCount
                ? totalMemberCount % skip
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMemberCount;

            base.m_initializeAction = InitializeMemberList()(list)(CurrentPageSize)(skip);

            base.m_initializeAction();
        }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        internal T[] ExportEnumerable() => m_pinedEnumerable;

        private Func<T[], Func<int, Func<int, Action>>> InitializeMemberList()
        {
            return array => size => skip => () =>
            {
                base.m_memberList = new List<IPageMember<T>>(size);
                for (var i = 0; i < size; i++)
                {
                    base.m_memberList.Add(new PageMember<T>(array[i], i, skip));
                }
            };
        }
    }
}

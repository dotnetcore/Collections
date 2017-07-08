using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public class EnumerablePage<T> : Page<T>
    {
        internal readonly IEnumerable<T> m_pinedEnumerable;

        public EnumerablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMembersCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;

            m_pinedEnumerable = enumerable;
            var list = enumerable.Skip(skip).Take(pageSize).ToList();

            base.TotalPagesCount = (int)Math.Ceiling((double)totalMembersCount / (double)pageSize);
            base.TotalMembersCount = totalMembersCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPagesCount
                ? totalMembersCount % skip
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMembersCount;

            for (var i = 0; i < CurrentPageSize; i++)
            {
                base.m_MemberList.Add(new PageMember<T>(list[skip + 1], i));
            }
        }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();
    }
}

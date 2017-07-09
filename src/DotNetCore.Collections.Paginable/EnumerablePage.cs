using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public class EnumerablePage<T> : PageBase<T>
    {
        internal readonly IEnumerable<T> m_pinedEnumerable;

        public EnumerablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMemberCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;

            m_pinedEnumerable = enumerable;
            var list = enumerable.Skip(skip).Take(pageSize).ToList();

            base.TotalPageCount = (int)Math.Ceiling((double)totalMemberCount / (double)pageSize);
            base.TotalMemberCount = totalMemberCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPageCount
                ? totalMemberCount % skip
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMemberCount;

            for (var i = 0; i < CurrentPageSize; i++)
            {
                base.m_memberList.Add(new PageMember<T>(list[skip + 1], i));
            }
        }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();
    }
}

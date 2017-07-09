using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    public class PaginableEnumerable<T> : PaginableSetBase<T>
    {
        private PaginableEnumerable() { }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount)
            : base(enumerable, pageSize, realMemberCount, realPageCount)
        {

        }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount, int limitedMemberCount)
            : base(enumerable, pageSize, realPageCount, realMemberCount, limitedMemberCount)
        {

        }
    }
}

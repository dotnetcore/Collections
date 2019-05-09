using System;
using Chloe;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// ChloePage collection
    /// </summary>
    public class PaginableChloeQuery<T> : PaginableSetBase<T>
    {
        private readonly IQuery<T> _freeSqlQuery;
        private readonly Func<IQuery<T>, IQuery<T>> _additionalQueryFunc;
        private PaginableChloeQuery() { }

        internal PaginableChloeQuery(IQuery<T> select, int pageSize, int realPageCount, int realMemberCount, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _freeSqlQuery = select;
            _additionalQueryFunc = additionalQueryFunc;
        }

        internal PaginableChloeQuery(IQuery<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _freeSqlQuery = select;
            _additionalQueryFunc = additionalQueryFunc;
        }

        protected override Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new Lazy<IPage<T>>(() => new ChloePage<T>(_freeSqlQuery, currentPageNumber, pageSize, realMemberCount, _additionalQueryFunc));
        }
    }
}

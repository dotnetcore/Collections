using System;
using Chloe;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// ChloePage collection
    /// </summary>
    public class PaginableChloeQuery<T> : PaginableSetBase<T>
    {
        private readonly IQuery<T> _chloeQuery;
        private readonly Func<IQuery<T>, IQuery<T>> _additionalQueryFunc;

        // ReSharper disable once UnusedMember.Local
        private PaginableChloeQuery() { }

        internal PaginableChloeQuery(IQuery<T> select, int pageSize, int realPageCount, int realMemberCount, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _chloeQuery = select;
            _additionalQueryFunc = additionalQueryFunc;
        }

        internal PaginableChloeQuery(IQuery<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount,
            Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _chloeQuery = select;
            _additionalQueryFunc = additionalQueryFunc;
        }

        /// <inheritdoc />
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new(() => new ChloePage<T>(_chloeQuery, currentPageNumber, pageSize, realMemberCount, _additionalQueryFunc));
        }
    }
}
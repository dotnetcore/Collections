using System;
using Dos.ORM;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// DosPage collection
    /// </summary>
    public class PaginableDosQuery<T> : PaginableSetBase<T> where T : Entity {
        private readonly FromSection<T> _dosOrmQuery;

        private readonly Func<FromSection<T>, FromSection<T>> _additionalQueryFunc;

        // ReSharper disable once UnusedMember.Local
        private PaginableDosQuery() { }

        internal PaginableDosQuery(FromSection<T> select, int pageSize, int realPageCount, int realMemberCount, Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null)
            : base(pageSize, realPageCount, realMemberCount) {
            _dosOrmQuery = select;
            _additionalQueryFunc = additionalQueryFunc;
        }

        internal PaginableDosQuery(FromSection<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount,
            Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount) {
            _dosOrmQuery = select;
            _additionalQueryFunc = additionalQueryFunc;
        }

        /// <inheritdoc />
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount) {
            return new Lazy<IPage<T>>(() =>
                new DosPage<T>(_dosOrmQuery, currentPageNumber, pageSize, realMemberCount, _additionalQueryFunc));
        }
    }
}
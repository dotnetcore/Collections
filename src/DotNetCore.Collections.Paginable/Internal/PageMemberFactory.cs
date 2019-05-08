using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetCore.Collections.Paginable.Abstractions;

namespace DotNetCore.Collections.Paginable.Internal
{
    public static class PageMemberFactory
    {
        public static PageMember<T> Create<T>(T memberValue, int offset, ref int startIndex)
            => new PageMember<T>(memberValue, offset, ref startIndex);

        public static PageMember<T> Create<T>(IEnumerable<T> memberColl, int index, int offset, ref int startIndex)
            => new PageMember<T>(memberColl.ElementAt(index), offset, ref startIndex);

        public static PageMember<T> Create<T>(IQueryEntryState<T> state, int offset, ref int startIndex)
            => new PageMember<T>(state, offset, ref startIndex);
    }
}

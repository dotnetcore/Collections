using System;

namespace DotNetCore.Collections.Paginable.Internal
{
    /// <summary>
    /// Paginable calculation utilities.
    /// Single source of truth for member-count limiting and page-count calculations,
    /// shared by the core factory and all ORM integration coll-factories.
    /// </summary>
    public static class PaginableCalc
    {
        /// <summary>
        /// Get real member count: apply the limited member count (if any) onto the real count.
        /// </summary>
        /// <param name="limitedMemberCount">limited member count (null or invalid means unlimited)</param>
        /// <param name="count">real member count from the data source</param>
        /// <returns></returns>
        public static int GetRealMemberCount(int? limitedMemberCount, int count)
            => limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? limitedMemberCount.Value > count
                    ? count
                    : limitedMemberCount.Value
                : count;

        /// <summary>
        /// Get real page count from real member count and page size.
        /// </summary>
        /// <param name="realMemberCount">real member count, which may have been limited by <see cref="GetRealMemberCount"/></param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static int GetRealPageCount(int realMemberCount, int pageSize)
            => (int) Math.Ceiling((double) realMemberCount / (double) pageSize);
    }
}

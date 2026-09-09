using System.Threading;
using System.Threading.Tasks;
using SqlSugar;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class SqlSugarHelper
    {
        public static int Count<T>(ISugarQueryable<T> query) => query.Count();

        // SqlSugar 5.1.3 exposes CountAsync() and CountAsync(Expression<...>) but no
        // CancellationToken overload; the token is accepted for API symmetry and will be
        // forwarded once the provider ships such an overload.
        public static Task<int> CountAsync<T>(ISugarQueryable<T> query, CancellationToken cancellationToken)
            => query.CountAsync();
    }
}

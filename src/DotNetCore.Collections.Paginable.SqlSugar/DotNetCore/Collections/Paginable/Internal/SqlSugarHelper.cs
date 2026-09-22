using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlSugar;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class SqlSugarHelper
    {
        public static int Count<T>(ISugarQueryable<T> query) => query.Count();

        // SqlSugar 5.1.3 exposes CountAsync() and CountAsync(Expression<...>) but no
        // CancellationToken overload, so a token can never reach the provider. It is therefore
        // honoured at the boundary this library owns: an already-cancelled token stops the call
        // before the first database round-trip instead of being silently dropped (F6-38). When the
        // provider ships a token overload, this is the only method that has to change.
        public static Task<int> CountAsync<T>(ISugarQueryable<T> query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return query.CountAsync();
        }

        // Same provider limitation as CountAsync: ToPageListAsync(int, int, RefAsync<int>) is the
        // only async overload in 5.1.3, so the token is honoured here as well (F6-38). Checking it
        // again at the fetch call also covers cancellation raised while the count was in flight.
        public static Task<List<T>> FetchPageAsync<T>(ISugarQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return query.ToPageListAsync(pageNumber, pageSize);
        }
    }
}

using System.Threading.Tasks;
using SqlKata;
using SqlKata.Execution;

namespace DotNetCore.Collections.Paginable.Internal {
    internal static class SqlKataHelper {
        public static int Count(Query query) => query.Clone().Count<int>();

        public static Task<int> CountAsync(Query query) => query.Clone().CountAsync<int>();
    }
}
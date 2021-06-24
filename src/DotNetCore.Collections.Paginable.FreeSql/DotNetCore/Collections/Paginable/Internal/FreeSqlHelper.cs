using System.Threading.Tasks;
using FreeSql;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class FreeSqlHelper
    {
        public static long Count<T>(ISelect<T> select) where T : class => select.Count();

        public static Task<long> CountAsync<T>(ISelect<T> select) where T : class => select.CountAsync();
    }
}
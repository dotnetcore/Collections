using System.Threading.Tasks;
using SqlSugar;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class SqlSugarHelper
    {
        public static int Count<T>(ISugarQueryable<T> query) => query.Count();

        public static Task<int> CountAsync<T>(ISugarQueryable<T> query) => query.CountAsync();
    }
}
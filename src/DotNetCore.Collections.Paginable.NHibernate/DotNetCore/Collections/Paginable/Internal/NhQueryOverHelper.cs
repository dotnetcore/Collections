using System.Threading.Tasks;
using NHibernate;

namespace DotNetCore.Collections.Paginable.Internal {
    internal static class NhQueryOverHelper {
        public static int Count<T>(IQueryOver<T> queryOver) => queryOver.RowCount();
        public static Task<int> CountAsync<T>(IQueryOver<T> queryOver) => queryOver.RowCountAsync();
    }
}
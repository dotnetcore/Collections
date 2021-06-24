using Chloe;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class ChloeHelper
    {
        public static int Count<T>(IQuery<T> query) => query.Count();
    }
}
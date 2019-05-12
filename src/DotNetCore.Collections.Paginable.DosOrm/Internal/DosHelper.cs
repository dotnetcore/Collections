using Dos.ORM;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class DosHelper
    {
        public static int Count<T>(FromSection<T> query) where T : Entity => query.Count();
    }
}
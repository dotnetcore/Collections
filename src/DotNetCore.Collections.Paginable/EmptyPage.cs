using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Empty page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EmptyPage<T> : EnumerablePage<T>
    {
        public EmptyPage() : base(Enumerable.Empty<T>(), 1, 0, 0) { }
    }
}

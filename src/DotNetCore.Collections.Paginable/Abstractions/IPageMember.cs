// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public interface IPageMember<out T>
    {
        T Value { get; }

        int Offset { get; }

        int ItemNumber { get; }
    }
}

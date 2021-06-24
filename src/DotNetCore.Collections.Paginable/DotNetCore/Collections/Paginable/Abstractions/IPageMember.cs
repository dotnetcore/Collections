// ReSharper disable once CheckNamespace

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Page member wrapper interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPageMember<out T>
    {
        /// <summary>
        /// Gets value of current member
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets offset of current member
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Gets item number of current member
        /// </summary>
        int ItemNumber { get; }
    }
}
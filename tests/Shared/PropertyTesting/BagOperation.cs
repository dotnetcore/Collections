using System;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// The operations a bag property suite can generate.
    /// </summary>
    public enum BagOperationKind
    {
        /// <summary>Adds one copy of the element.</summary>
        Add = 0,

        /// <summary>Adds several copies of the element at once.</summary>
        AddCopies = 1,

        /// <summary>Removes a single copy of the element.</summary>
        RemoveOne = 2,

        /// <summary>Removes up to several copies of the element.</summary>
        RemoveCopies = 3,

        /// <summary>Removes every copy of the element.</summary>
        RemoveAllCopies = 4,

        /// <summary>Removes everything.</summary>
        Clear = 5
    }

    /// <summary>
    /// One generated bag operation: a kind, the element it targets and (for the counted forms) how
    /// many copies it carries.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// A plain mutable struct with public fields keeps the type C# 7.3 compatible and keeps the
    /// failure report cheap to render.
    /// </remarks>
    public struct BagOperation<T>
    {
        /// <summary>The operation kind.</summary>
        public BagOperationKind Kind;

        /// <summary>The element the operation targets; ignored by <see cref="BagOperationKind.Clear"/>.</summary>
        public T Item;

        /// <summary>The copy count for the counted forms; ignored otherwise.</summary>
        public int Times;
    }
}

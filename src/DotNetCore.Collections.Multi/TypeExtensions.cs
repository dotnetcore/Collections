using System;
using System.Text;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Selects how <see cref="TypeExtensions.ToReadableString(Type, TypeNameFormat)"/> spells a type.
    /// </summary>
    public enum TypeNameFormat
    {
        /// <summary>
        /// The CLR spelling with namespaces removed, e.g.
        /// <c>Dictionary&lt;DateTimeOffset,IReadOnlyDictionary&lt;String,Int32&gt;&gt;[,]</c>.
        /// This is the default.
        /// </summary>
        Clr = 0,

        /// <summary>
        /// The C# spelling: primitive aliases become keywords and <see cref="Nullable{T}"/> becomes
        /// <c>T?</c>, e.g. <c>Dictionary&lt;DateTimeOffset,IReadOnlyDictionary&lt;string,int&gt;&gt;[,]</c>.
        /// </summary>
        CSharp = 1,
    }

    /// <summary>
    /// <see cref="Type"/> formatting helpers (F6-30): a one-line, human-readable spelling of a type,
    /// for diagnostics, log messages and test failure output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default <see cref="TypeNameFormat.Clr"/> spelling is lossy in exactly one way —
    /// namespaces are dropped — because that is what makes a long generic type readable:
    /// <c>Dictionary&lt;DateTimeOffset,IReadOnlyDictionary&lt;String,Int32&gt;&gt;[,]</c> instead of the
    /// fully qualified form. Array ranks are kept, generic arguments are rendered recursively, and
    /// nested types are joined with <c>.</c> rather than the reflection <c>+</c> separator.
    /// </para>
    /// <para>
    /// Recursion is bounded: <see cref="DefaultMaxDepth"/> levels of array / generic nesting are
    /// rendered and anything deeper collapses to <c>...</c>. A type built through reflection can
    /// nest arbitrarily deep — nothing in the CLR stops <c>MakeGenericType</c> from being applied in
    /// a loop — so an unbounded walk would be a stack overflow waiting to happen in the middle of a
    /// diagnostic message.
    /// </para>
    /// <para>
    /// The output is stable: the same <see cref="Type"/> always produces the same string, so it is
    /// safe to assert on in tests.
    /// </para>
    /// <para>
    /// One simplification is worth stating: for a nested generic type the argument list rendered is
    /// what <c>Type.GetGenericArguments()</c> returns, which covers the declaring type's parameters
    /// as well; the declaring chain itself is spelled by name only.
    /// </para>
    /// </remarks>
    public static class TypeExtensions
    {
        /// <summary>
        /// The number of array / generic nesting levels the parameterless
        /// <see cref="ToReadableString(Type)"/> renders before collapsing the remainder to
        /// <c>...</c>. Sixteen levels is already deeper than any hand-written type name.
        /// </summary>
        public const int DefaultMaxDepth = 16;

        /// <summary>
        /// Returns the readable, namespace-free spelling of <paramref name="type"/>, e.g.
        /// <c>Dictionary&lt;DateTimeOffset,IReadOnlyDictionary&lt;String,Int32&gt;&gt;[,]</c>.
        /// </summary>
        /// <param name="type">The type to spell. Must not be <c>null</c>.</param>
        /// <returns>A stable, human-readable name; never <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static string ToReadableString(this Type type)
        {
            return ToReadableString(type, TypeNameFormat.Clr, DefaultMaxDepth);
        }

        /// <summary>
        /// Returns the readable spelling of <paramref name="type"/> in the requested
        /// <paramref name="format"/>.
        /// </summary>
        /// <param name="type">The type to spell. Must not be <c>null</c>.</param>
        /// <param name="format">The spelling to use.</param>
        /// <returns>A stable, human-readable name; never <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public static string ToReadableString(this Type type, TypeNameFormat format)
        {
            return ToReadableString(type, format, DefaultMaxDepth);
        }

        /// <summary>
        /// Returns the readable spelling of <paramref name="type"/> in the requested
        /// <paramref name="format"/>, rendering at most <paramref name="maxDepth"/> levels of
        /// array / generic nesting.
        /// </summary>
        /// <param name="type">The type to spell. Must not be <c>null</c>.</param>
        /// <param name="format">The spelling to use.</param>
        /// <param name="maxDepth">
        /// The number of nesting levels to render before collapsing the remainder to <c>...</c>.
        /// Must be at least 1; the other overloads pass <see cref="DefaultMaxDepth"/>.
        /// </param>
        /// <returns>A stable, human-readable name; never <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxDepth"/> is less than 1.
        /// </exception>
        public static string ToReadableString(this Type type, TypeNameFormat format, int maxDepth)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (maxDepth < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDepth), maxDepth, "The nesting depth must be at least 1.");
            }

            var builder = new StringBuilder();
            Append(builder, type, format, maxDepth, 0);
            return builder.ToString();
        }

        private static void Append(
            StringBuilder builder, Type type, TypeNameFormat format, int maxDepth, int depth)
        {
            if (depth >= maxDepth)
            {
                builder.Append("...");
                return;
            }

            // By-ref and pointer types carry their element in GetElementType(); the CLR's own
            // Name for them ("Int32&" / "Int32*") would leave the arity suffix of a generic
            // element in place, so both are rebuilt from the element instead.
            if (type.IsByRef || type.IsPointer)
            {
                Append(builder, type.GetElementType()!, format, maxDepth, depth + 1);
                builder.Append(type.IsByRef ? '&' : '*');
                return;
            }

            if (type.IsArray)
            {
                // The rank specifiers are emitted outermost first, which is the order C# source
                // writes them in: the CLR models string[,][] as a rank-2 array whose element is a
                // rank-1 array, and "String[,][]" is what a reader expects to see. Appending each
                // suffix on the way back up the recursion would produce "String[][,]" instead.
                var suffixes = new StringBuilder();
                var element = type;
                var levels = 0;
                while (element.IsArray)
                {
                    suffixes.Append('[');
                    suffixes.Append(',', element.GetArrayRank() - 1);
                    suffixes.Append(']');
                    element = element.GetElementType()!;
                    levels++;
                }

                Append(builder, element, format, maxDepth, depth + levels);
                builder.Append(suffixes.ToString());
                return;
            }

            if (format == TypeNameFormat.CSharp)
            {
                var alias = TryGetCSharpAlias(type);
                if (alias != null)
                {
                    builder.Append(alias);
                    return;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Append(builder, type.GetGenericArguments()[0], format, maxDepth, depth + 1);
                    builder.Append('?');
                    return;
                }
            }

            AppendSimpleName(builder, type);

            if (!type.IsGenericType)
            {
                return;
            }

            var arguments = type.GetGenericArguments();
            builder.Append('<');
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                Append(builder, arguments[i], format, maxDepth, depth + 1);
            }

            builder.Append('>');
        }

        private static void AppendSimpleName(StringBuilder builder, Type type)
        {
            // A generic parameter is declared *by* its owner, so DeclaringType is set and the
            // nested-type walk below would spell it "List.T"; the parameter is just "T".
            if (type.IsGenericParameter)
            {
                builder.Append(type.Name);
                return;
            }

            if (type.IsNested && type.DeclaringType != null)
            {
                AppendSimpleName(builder, type.DeclaringType);
                builder.Append('.');
            }

            var name = type.Name;
            var arity = name.IndexOf('`');
            builder.Append(arity < 0 ? name : name.Substring(0, arity));
        }

        private static string? TryGetCSharpAlias(Type type)
        {
            switch (type.FullName)
            {
                case "System.Boolean":
                    return "bool";
                case "System.Byte":
                    return "byte";
                case "System.SByte":
                    return "sbyte";
                case "System.Char":
                    return "char";
                case "System.Decimal":
                    return "decimal";
                case "System.Double":
                    return "double";
                case "System.Single":
                    return "float";
                case "System.Int16":
                    return "short";
                case "System.UInt16":
                    return "ushort";
                case "System.Int32":
                    return "int";
                case "System.UInt32":
                    return "uint";
                case "System.Int64":
                    return "long";
                case "System.UInt64":
                    return "ulong";
                case "System.Object":
                    return "object";
                case "System.String":
                    return "string";
                case "System.Void":
                    return "void";
                default:
                    return null;
            }
        }
    }
}

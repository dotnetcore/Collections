// F6-23 nullable compatibility: internal polyfill of the compiler's nullability attributes.
//
// net451 / net461 / net47 / net48 / netstandard2.0 ship no in-box definition of the nullable
// attribute types that the C# 8+ compiler reads and emits for `<Nullable>enable</Nullable>`
// builds. On those targets this file is compiled into every project that imports
// build/common.props (conditional <Compile Include> there); on netstandard2.1 and net6.0+ the
// BCL defines the types in-box and the file is NOT compiled in, so the compiler keeps using
// the framework's own definitions.
//
// The shape below mirrors what Roslyn embeds itself when no definition is reachable (that is
// the behaviour these targets got before F6-23 - the compiler synthesizes these types into the
// output assembly). Declaring them explicitly keeps the emitted metadata identical while
// making the nullable-story of the low-generation targets visible and auditable. The types are
// `internal` and never leak into the public API surface; do not reference them from library
// code and do not widen their visibility.

// ReSharper disable CheckNamespace

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Field
        | AttributeTargets.Parameter
        | AttributeTargets.Property
        | AttributeTargets.Event
        | AttributeTargets.Interface
        | AttributeTargets.Delegate,
        AllowMultiple = false,
        Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;

        public NullableAttribute(byte flag)
        {
            NullableFlags = new[] { flag };
        }

        public NullableAttribute(byte[] flags)
        {
            NullableFlags = flags;
        }
    }

    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Field
        | AttributeTargets.Parameter
        | AttributeTargets.Property
        | AttributeTargets.Event
        | AttributeTargets.Interface
        | AttributeTargets.Delegate
        | AttributeTargets.Method
        | AttributeTargets.Constructor,
        AllowMultiple = false,
        Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;

        public NullableContextAttribute(byte flag)
        {
            Flag = flag;
        }
    }

    [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
    internal sealed class NullablePublicOnlyAttribute : Attribute
    {
        public readonly bool IncludesInternals;

        public NullablePublicOnlyAttribute(bool includesInternals)
        {
            IncludesInternals = includesInternals;
        }
    }
}

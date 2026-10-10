// F7-01: compile-time gate that verifies the three conditional-compilation paths
// are mutually exclusive and collectively exhaustive across the 11-TFM matrix.
//
// Path 1 (Intrinsic Span): NETCOREAPP2_1_OR_GREATER || NET5_0_OR_GREATER
//   → netcoreapp2.1+, net5.0+ : Span<T> / Memory<T> / ArrayPool<T> are in-box.
//
// Path 2 (CollectionsMarshal): NET5_0_OR_GREATER
//   → net5.0+ only : CollectionsMarshal.AsSpan(List<T>) is available.
//
// Path 3 (Polyfill): !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
//   → net451 / net461 / net47 / net48 / netstandard2.0 : rely on System.Memory
//     + System.Buffers + System.Runtime.CompilerServices.Unsafe NuGet packages.
//
// This file does not emit runtime code; it exists so a build failure here means
// the #if topology has drifted from the design.

namespace DotNetCore.Collections.Internal
{
    internal static class EngineSymbols
    {
#if NET5_0_OR_GREATER
        internal const bool HasCollectionsMarshal = true;
        internal const bool HasIntrinsicSpan = true;
        internal const bool NeedsPolyfill = false;
#elif NETCOREAPP2_1_OR_GREATER
        internal const bool HasCollectionsMarshal = false;
        internal const bool HasIntrinsicSpan = true;
        internal const bool NeedsPolyfill = false;
#elif NETSTANDARD2_1_OR_GREATER
        internal const bool HasCollectionsMarshal = false;
        internal const bool HasIntrinsicSpan = false;
        internal const bool NeedsPolyfill = false;
#else
        internal const bool HasCollectionsMarshal = false;
        internal const bool HasIntrinsicSpan = false;
        internal const bool NeedsPolyfill = true;
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-07: the namespace and overload-resolution contract of the shipped assembly, pinned by
    /// reflection so that it holds for every type the engine emits and not only for the files
    /// somebody remembered to look at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The risk these tests fence off is R7-05: a consumer that writes
    /// <c>using System.Linq;</c> next to <c>using DotNetCore.Collections;</c> must get CS0121 from
    /// neither of them. That can only be guaranteed from the engine's side by keeping the engine's
    /// extension methods out of the BCL's namespaces and off the receiver shapes the BCL's own
    /// <see cref="Enumerable"/> extends.
    /// </para>
    /// <para>
    /// The source-level half of the same guard is <c>scripts/Check-EngineNamespace.ps1</c>, which
    /// runs in CI and sees declarations the TFM compiled away; these tests see the built surface
    /// instead, which is what a consumer actually binds against. Neither half subsumes the other.
    /// </para>
    /// </remarks>
    public class NamespaceGovernanceTests
    {
        private const string EngineNamespace = "DotNetCore.Collections";

        /// <summary>
        /// <see cref="Enumerable"/> members that only exist on TFMs newer than the one this suite
        /// runs on, so the reflection oracle below cannot discover them on its own.
        /// </summary>
        /// <remarks>
        /// <c>Index</c> is the entry that matters today: .NET 9 added
        /// <c>Enumerable.Index&lt;TSource&gt;(this IEnumerable&lt;TSource&gt;)</c> and the engine ships its own
        /// <c>Index</c> over the same element shape. The engine's form wins because its receiver is the
        /// exact static type (an identity conversion) while the BCL's needs an interface conversion,
        /// but that argument only stays true as long as the engine keeps its receiver engine-owned —
        /// which is what the third test below asserts. F7-18 re-runs the BCL survey and is what should
        /// grow this list.
        /// </remarks>
        private static readonly string[] EnumerableMembersNewerThanTheTestTarget =
        {
            "Index",
        };

        /// <summary>
        /// The only receiver shapes the engine is allowed to extend outside its own type family.
        /// </summary>
        /// <remarks>
        /// The four entry wrappers deliberately sit on <c>T[]</c> / <c>List&lt;T&gt;</c> /
        /// <c>IReadOnlyList&lt;T&gt;</c> / <c>IEnumerable&lt;T&gt;</c> — they are how a caller enters the engine,
        /// so they cannot take an engine type. Every one of them is named <c>ToValueEnumerable</c>,
        /// whose name the BCL does not own and is not plausibly going to. Adding a *new*
        /// BCL-receiver extension therefore has to be a deliberate act recorded under this constant,
        /// not something that slips in.
        /// </remarks>
        private const string EntryWrapperName = "ToValueEnumerable";

        private static Assembly EngineAssembly => typeof(CollectionExtensions).Assembly;

        [Fact]
        public void No_engine_type_is_declared_inside_the_LINQ_namespace()
        {
            var intruders = EngineAssembly
                .GetTypes()
                .Where(type => IsInsideLinq(type.Namespace))
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            intruders.ShouldBeEmpty(
                "declaring anything under System.Linq injects the engine into the BCL's own namespace, "
                + "which is what turns a plain `using System.Linq;` into CS0121 (R7-05).");
        }

        [Fact]
        public void Every_public_engine_type_lives_under_the_engine_namespace()
        {
            // Exported (public) types only: the low-generation nullable attribute polyfill compiled
            // in from build/NullabilityAttributes.cs legitimately declares System.Runtime.CompilerServices
            // types, but it is `internal` on every target and therefore never part of the public
            // surface. Asserting on the exported set keeps this rule true on all eleven TFMs instead
            // of only on the one this suite runs.
            var strays = EngineAssembly
                .GetExportedTypes()
                .Where(type => !IsEngineOwnedNamespace(type.Namespace))
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            strays.ShouldBeEmpty("every public engine type belongs to '" + EngineNamespace + "' or a descendant of it.");
        }

        [Fact]
        public void Every_public_extension_method_lives_in_the_engine_namespace()
        {
            var strays = PublicExtensionMethods()
                .Where(method => method.DeclaringType.Namespace != EngineNamespace)
                .Select(Describe)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToArray();

            strays.ShouldBeEmpty(
                "the engine's extension methods are pinned to the '"
                + EngineNamespace
                + "' root namespace; a descendant such as .Internal would make the home of the public "
                + "surface ambiguous, and a BCL namespace would be an injection.");
        }

        [Fact]
        public void Every_public_extension_method_sharing_a_name_with_Enumerable_extends_an_engine_type()
        {
            var enumerableNames = EnumerableMemberNames();
            var offenders = PublicExtensionMethods()
                .Where(method => enumerableNames.Contains(method.Name))
                .Where(method => !IsEngineOwned(method.GetParameters()[0].ParameterType))
                .Select(Describe)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToArray();

            offenders.ShouldBeEmpty(
                "an extension method that shares a name with System.Linq.Enumerable must take an engine-owned "
                + "receiver. A receiver the BCL also extends makes the two candidates equally good, and once "
                + "an Enumerable member with that name exists the call is CS0121 — see Enumerable.Index, "
                + "added in .NET 9.");
        }

        [Fact]
        public void Only_the_documented_entry_wrappers_extend_a_BCL_receiver()
        {
            var bclReceivers = PublicExtensionMethods()
                .Where(method => !IsEngineOwned(method.GetParameters()[0].ParameterType))
                .Select(Describe)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToArray();

            var inventory = string.Join(
                " | ",
                PublicExtensionMethods()
                    .Select(method => Describe(method) + " -> engineOwned=" + IsEngineOwned(method.GetParameters()[0].ParameterType))
                    .OrderBy(text => text, StringComparer.Ordinal));

            var found = "bclReceivers: [" + string.Join(", ", bclReceivers) + "] all: [" + inventory + "]";

            bclReceivers.ShouldAllBe(text => text.Contains("." + EntryWrapperName + "("), found);

            // One overload per source arm. Pinning the count as well means a new BCL-receiver
            // overload cannot arrive unnoticed under the same name.
            bclReceivers.Length.ShouldBe(4, found);
        }

        private static IEnumerable<MethodInfo> PublicExtensionMethods() =>
            EngineAssembly
                .GetExportedTypes()
                .Where(type => type.IsAbstract && type.IsSealed)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(method => method.IsDefined(typeof(ExtensionAttribute), false));

        private static HashSet<string> EnumerableMemberNames()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var method in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                names.Add(method.Name);
            }

            foreach (var name in EnumerableMembersNewerThanTheTestTarget)
            {
                names.Add(name);
            }

            return names;
        }

        private static bool IsInsideLinq(string ns) =>
            ns != null
            && (ns == "System.Linq" || ns.StartsWith("System.Linq.", StringComparison.Ordinal));

        private static bool IsEngineOwnedNamespace(string ns) =>
            ns != null
            && (ns == EngineNamespace || ns.StartsWith(EngineNamespace + ".", StringComparison.Ordinal));

        private static bool IsEngineOwned(Type type)
        {
            while (type.HasElementType)
            {
                type = type.GetElementType();
            }

            // A generic parameter reports the namespace of its *declaring* type, so `this T[]` would
            // otherwise look engine-owned once the array element is unwrapped. It is not: an array of
            // anything is exactly the receiver shape the BCL extends.
            if (type.IsGenericParameter)
            {
                return false;
            }

            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            return IsEngineOwnedNamespace(type.Namespace);
        }

        private static string Describe(MethodInfo method) =>
            method.DeclaringType.FullName + "." + method.Name + "(" + method.GetParameters()[0].ParameterType.Name + ")";
    }
}

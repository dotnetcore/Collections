using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-30: <see cref="TypeExtensions.ToReadableString(Type)"/> - the readable type-name
    /// formatter. Covers the reference shape (a two-dimensional array of a nested generic), array
    /// ranks, open generics, nested types (joined with <c>.</c>, never the reflection <c>+</c>
    /// separator), the C# keyword / nullable spelling, the bounded recursion that stops a
    /// reflection-built deep type from overflowing the stack, output stability, and the two guards.
    /// </summary>
    public class TypeExtensionsTests
    {
        [Fact]
        public void ToReadableString_ReferenceShape_ProducesTheExpectedName()
        {
            var type = typeof(Dictionary<DateTimeOffset, IReadOnlyDictionary<string, int>>[,]);

            var text = type.ToReadableString();

            text.ShouldBe("Dictionary<DateTimeOffset,IReadOnlyDictionary<String,Int32>>[,]");
            text.ShouldNotContain("System.");
        }

        [Fact]
        public void ToReadableString_ArrayRanks_ArePreserved()
        {
            typeof(int[]).ToReadableString().ShouldBe("Int32[]");
            typeof(int[,]).ToReadableString().ShouldBe("Int32[,]");
            typeof(int[,,]).ToReadableString().ShouldBe("Int32[,,]");
            typeof(int[][]).ToReadableString().ShouldBe("Int32[][]");
            // The rank specifiers are emitted in C# source order (outermost first), so the two
            // jagged shapes below are not interchangeable.
            typeof(string[,][]).ToReadableString().ShouldBe("String[,][]");
            typeof(string[][,]).ToReadableString().ShouldBe("String[][,]");
        }

        [Fact]
        public void ToReadableString_OpenGeneric_KeepsItsParameters()
        {
            typeof(List<>).ToReadableString().ShouldBe("List<T>");
            typeof(Dictionary<,>).ToReadableString().ShouldBe("Dictionary<TKey,TValue>");
        }

        [Fact]
        public void ToReadableString_NestedType_IsJoinedWithADot()
        {
            var text = typeof(NestedOuter.Inner).ToReadableString();

            text.ShouldBe("NestedOuter.Inner");
            text.ShouldNotContain("+");
        }

        [Fact]
        public void ToReadableString_ByRefAndPointer_KeepTheirSuffix()
        {
            typeof(int).MakeByRefType().ToReadableString().ShouldBe("Int32&");
            typeof(int).MakePointerType().ToReadableString().ShouldBe("Int32*");
            typeof(List<int>).MakeByRefType().ToReadableString().ShouldBe("List<Int32>&");
        }

        [Fact]
        public void ToReadableString_CSharpFormat_MapsKeywordsAndNullable()
        {
            typeof(Dictionary<DateTimeOffset, IReadOnlyDictionary<string, int>>[,])
                .ToReadableString(TypeNameFormat.CSharp)
                .ShouldBe("Dictionary<DateTimeOffset,IReadOnlyDictionary<string,int>>[,]");

            typeof(int?).ToReadableString(TypeNameFormat.CSharp).ShouldBe("int?");
            typeof(int?).ToReadableString().ShouldBe("Nullable<Int32>");
            typeof(bool).ToReadableString(TypeNameFormat.CSharp).ShouldBe("bool");
            typeof(decimal).ToReadableString(TypeNameFormat.CSharp).ShouldBe("decimal");
            typeof(object).ToReadableString(TypeNameFormat.CSharp).ShouldBe("object");
        }

        [Fact]
        public void ToReadableString_WithinTheDepthBudget_RendersEveryLevel()
        {
            typeof(List<List<List<int>>>).ToReadableString().ShouldBe("List<List<List<Int32>>>");
            TypeExtensions.DefaultMaxDepth.ShouldBe(16);
        }

        [Fact]
        public void ToReadableString_BeyondTheDepthBudget_CollapsesToEllipsis()
        {
            typeof(List<List<List<int>>>)
                .ToReadableString(TypeNameFormat.Clr, 2)
                .ShouldBe("List<List<...>>");
        }

        [Fact]
        public void ToReadableString_SelfReferentialNesting_DoesNotOverflowTheStack()
        {
            Type type = typeof(int);
            for (var i = 0; i < 128; i++)
            {
                type = typeof(List<>).MakeGenericType(type);
            }

            var text = type.ToReadableString();

            text.ShouldContain("...");
            text.ShouldStartWith("List<List<");
            text.Length.ShouldBeLessThan(150);
        }

        [Fact]
        public void ToReadableString_IsStable()
        {
            var type = typeof(Dictionary<DateTimeOffset, IReadOnlyDictionary<string, int>>[,]);
            var element = type.GetElementType()!;

            type.ToReadableString().ShouldBe(type.ToReadableString());
            element.ToReadableString()
                .ShouldBe("Dictionary<DateTimeOffset,IReadOnlyDictionary<String,Int32>>");
        }

        [Fact]
        public void ToReadableString_NullType_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => ((Type)null!).ToReadableString());
        }

        [Fact]
        public void ToReadableString_NonPositiveMaxDepth_ThrowsArgumentOutOfRangeException()
        {
            Should.Throw<ArgumentOutOfRangeException>(
                () => typeof(int).ToReadableString(TypeNameFormat.Clr, 0));
        }
    }

    internal class NestedOuter
    {
        public class Inner
        {
        }
    }
}

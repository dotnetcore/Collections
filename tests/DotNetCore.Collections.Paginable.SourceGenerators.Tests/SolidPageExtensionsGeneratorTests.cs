using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.SourceGenerators.Tests;

public class SolidPageExtensionsGeneratorTests
{
    private static (string generatedSource, ImmutableArray<Diagnostic> diagnostics) RunGenerator(string stubSource)
        => GeneratorHarness.Run(stubSource);

    [Fact]
    public void PostInitialization_EmitsAttributeDefinition()
    {
        var stubSource = """
namespace TestNamespace
{
    public static partial class SolidPageExtensions { }
}
""";
        var (_, diagnostics) = RunGenerator(stubSource);
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void BasicConfig_GeneratesToPaginableAndGetPage()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNamespace.FakeSource<T>",
        sourceParamName: "source",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTestQuery",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper")]
    public static partial class SolidPageExtensions { }
}

namespace TestNamespace
{
    public class FakeSource<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldNotBeEmpty();
        source.ShouldContain("partial class SolidPageExtensions");
        source.ShouldContain("ToPaginable<T>(this FakeSource<T> source");
        source.ShouldContain("PaginableTestQuery<T> ToPaginable<T>");
        source.ShouldContain("TestFactory.CreatePageSet(source");
        source.ShouldContain("IPage<T> GetPage<T>(this FakeSource<T> source");
        source.ShouldContain("new TestPage<T>(source, pageNumber, pageSize, TestHelper.Count(source))");
        source.ShouldContain("using TestNamespace;");
    }

    [Fact]
    public void HasAdditionalQueryFunc_GeneratesExtraParameter()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "Chloe",
        sourceTypeFullName: "TestNs.IQuery<T>",
        sourceParamName: "query",
        pageTypeName: "ChloePage",
        paginableTypeName: "PaginableChloeQuery",
        factoryTypeName: "PaginableChloeCollFactory",
        helperTypeName: "ChloeHelper",
        HasAdditionalQueryFunc = true)]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface IQuery<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("Func<IQuery<T>, IQuery<T>>? additionalQueryFunc = null");
        source.ShouldContain("additionalQueryFunc: additionalQueryFunc");

        var toPaginableMembers = source.Split('\n')
            .Where(l => l.Contains("ToPaginable<T>(this") && !l.Contains("ToPaginableAsync"))
            .ToList();
        toPaginableMembers.ShouldNotBeEmpty();
        toPaginableMembers.ShouldAllBe(l => !l.Contains("additionalQueryFunc"));
    }

    [Fact]
    public void ToPaginableHasAdditionalQueryFunc_TakesAndForwardsDelegate()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "DosORM",
        sourceTypeFullName: "TestNs.FromSection<T>",
        sourceParamName: "query",
        pageTypeName: "DosPage",
        paginableTypeName: "PaginableDosQuery",
        factoryTypeName: "PaginableDosCollFactory",
        helperTypeName: "DosHelper",
        GenericConstraint = "Entity",
        HasAdditionalQueryFunc = true,
        ToPaginableHasAdditionalQueryFunc = true)]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public class FromSection<T> { }
    public class Entity { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        var toPaginableMembers = source.Split('\n')
            .Where(l => l.Contains("PaginableDosQuery<T> ToPaginable<T>(this") || l.Contains("CreatePageSet(query"))
            .ToList();

        toPaginableMembers.ShouldAllBe(l => l.Contains("additionalQueryFunc"));
        source.ShouldContain("CreatePageSet(query, limitedMemberCount: limitedMemberCount, additionalQueryFunc: additionalQueryFunc)");
        source.ShouldContain("CreatePageSet(query, pageSize, limitedMemberCount, additionalQueryFunc: additionalQueryFunc)");
    }

    [Fact]
    public void ToPaginableSourceParamName_OverridesOnlyTheToPaginableFamily()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "NHibernate",
        sourceTypeFullName: "TestNs.IQueryOver<T>",
        sourceParamName: "queryOver",
        pageTypeName: "NhCorePage",
        paginableTypeName: "PaginableNhCoreQuery",
        factoryTypeName: "PaginableNhCoreCollFactory",
        helperTypeName: "NhQueryOverHelper",
        ToPaginableSourceParamName = "query")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface IQueryOver<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("ToPaginable<T>(this IQueryOver<T> query,");
        source.ShouldContain("GetPage<T>(this IQueryOver<T> queryOver,");
        source.ShouldNotContain("ToPaginable<T>(this IQueryOver<T> queryOver,");
    }

    [Fact]
    public void HasIncludeNestedMembers_GeneratesExtraParameter()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "FreeSql",
        sourceTypeFullName: "TestNs.ISelect<T>",
        sourceParamName: "select",
        pageTypeName: "FreeSqlPage",
        paginableTypeName: "PaginableFreeSqlQuery",
        factoryTypeName: "PaginableFreeSqlCollFactory",
        helperTypeName: "FreeSqlHelper",
        HasIncludeNestedMembers = true)]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface ISelect<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("bool? includeNestedMembers = null");
        source.ShouldContain("bool includeNestedMembers = false");
        source.ShouldContain("includeNestedMembers: includeNestedMembers");
    }

    [Fact]
    public void HasAsync_GeneratesToPaginableAsync()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "SqlSugar",
        sourceTypeFullName: "TestNs.ISugarQueryable<T>",
        sourceParamName: "query",
        pageTypeName: "SqlSugarPage",
        paginableTypeName: "PaginableSqlSugarQuery",
        factoryTypeName: "PaginableSqlSugarCollFactory",
        helperTypeName: "SqlSugarHelper",
        HasAsync = true,
        AsyncCountExpression = "await SqlSugarHelper.CountAsync(query, cancellationToken)",
        AsyncFetchExpression = "await query.ToPageListAsync(pageNumber, pageSize)",
        AsyncFetchReturnsList = true)]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface ISugarQueryable<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("ToPaginableAsync<T>");
        source.ShouldContain("PaginableSqlSugarCollFactory.CreatePageSetAsync");
        source.ShouldContain("GetPageAsync<T>");
        source.ShouldContain("await SqlSugarHelper.CountAsync(query, cancellationToken)");
        source.ShouldContain("var members = await query.ToPageListAsync(pageNumber, pageSize)");
        source.ShouldContain("new EnumerablePage<T>(members");
    }

    [Fact]
    public void AsyncCancellationToken_IsForwardedByDefault()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNs.IQuery<T>",
        sourceParamName: "query",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTestQuery",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper",
        HasAsync = true,
        AsyncCountExpression = "await TestHelper.CountAsync(query, cancellationToken)",
        AsyncFetchExpression = "await TestHelper.FetchPageAsync(query, pageNumber, pageSize, cancellationToken)",
        AsyncFetchReturnsList = true)]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface IQuery<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("TestFactory.CreatePageSetAsync(query, pageSize, limitedMemberCount, cancellationToken)");
        source.ShouldContain("GetPageAsync(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize, cancellationToken)");
        source.ShouldContain("var members = await TestHelper.FetchPageAsync(query, pageNumber, pageSize, cancellationToken)");
    }

    [Fact]
    public void ForwardAsyncCancellationToken_False_DropsTheTokenFromTheAsyncEntryPoints()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNs.IQuery<T>",
        sourceParamName: "query",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTestQuery",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper",
        HasAsync = true,
        AsyncCountExpression = "await TestHelper.CountAsync(query, cancellationToken)",
        AsyncFetchExpression = "await TestHelper.FetchPageAsync(query, pageNumber, pageSize, cancellationToken)",
        AsyncFetchReturnsList = true,
        ForwardAsyncCancellationToken = false)]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface IQuery<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("TestFactory.CreatePageSetAsync(query, pageSize, limitedMemberCount)");
        source.ShouldContain("GetPageAsync(query, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize)");
        source.ShouldNotContain("CreatePageSetAsync(query, pageSize, limitedMemberCount, cancellationToken)");
        source.ShouldNotContain("DefaultPageSize, cancellationToken)");

        // The slot governs forwarding at the extension boundary only: the caller-supplied count
        // expression keeps whatever token it names.
        source.ShouldContain("await TestHelper.CountAsync(query, cancellationToken)");
    }

    [Fact]
    public void AsyncFetchWithoutReturnsList_ReturnsExpressionDirectly()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNs.IQuery<T>",
        sourceParamName: "query",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTestQuery",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper",
        AsyncCountExpression = "await TestHelper.CountAsync(query)",
        AsyncFetchExpression = "await TestHelper.FetchPageAsync(query, pageNumber, pageSize)")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface IQuery<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("GetPageAsync<T>");
        source.ShouldContain("return await TestHelper.FetchPageAsync(query, pageNumber, pageSize)");
        source.ShouldNotContain("EnumerablePage<T>");
    }

    [Fact]
    public void SourceTypeIsNonGeneric_EmitsExplicitTypeParameter()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "SqlKata",
        sourceTypeFullName: "SqlKata.Query",
        sourceParamName: "query",
        pageTypeName: "SqlKataPage",
        paginableTypeName: "PaginableSqlKataQuery",
        factoryTypeName: "PaginableSqlKataCollFactory",
        helperTypeName: "SqlKataHelper",
        SourceTypeIsNonGeneric = true)]
    public static partial class SolidPageExtensions { }
}

namespace SqlKata
{
    public class Query { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("CreatePageSet<T>(query");
        source.ShouldContain("GetPage<T>(query");
    }

    [Fact]
    public void SyncCountCastExpression_AppendsCastToCount()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "FreeSql",
        sourceTypeFullName: "TestNs.ISelect<T>",
        sourceParamName: "select",
        pageTypeName: "FreeSqlPage",
        paginableTypeName: "PaginableFreeSqlQuery",
        factoryTypeName: "PaginableFreeSqlCollFactory",
        helperTypeName: "FreeSqlHelper",
        SyncCountCastExpression = ".AsInt32()")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface ISelect<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("FreeSqlHelper.Count(select).AsInt32()");
    }

    [Fact]
    public void GenericConstraint_EmitsWhereClause()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "DosORM",
        sourceTypeFullName: "TestNs.FromSection<T>",
        sourceParamName: "query",
        pageTypeName: "DosPage",
        paginableTypeName: "PaginableDosQuery",
        factoryTypeName: "PaginableDosCollFactory",
        helperTypeName: "DosHelper",
        GenericConstraint = "Entity")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public class FromSection<T> { }
    public class Entity { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("where T : Entity");
    }

    [Fact]
    public void GetPage_IncludesNullAndRangeChecks()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNs.ISource<T>",
        sourceParamName: "src",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTest",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface ISource<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldContain("if (src is null)");
        source.ShouldContain("throw new global::System.ArgumentNullException(nameof(src)");
        source.ShouldContain("if (pageNumber < 1)");
        source.ShouldContain("if (pageSize < 1)");
    }

    [Fact]
    public void GeneratedSource_ContainsAutoGeneratedHeader()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNs.ISource<T>",
        sourceParamName: "src",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTest",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface ISource<T> { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldStartWith("// <auto-generated/>");
        source.ShouldContain("#nullable enable");
    }

    [Fact]
    public void NoAttributeOnClass_NoGeneratedOutput()
    {
        var stubSource = """
namespace TestNamespace
{
    public static partial class SolidPageExtensions { }
}
""";
        var (source, _) = RunGenerator(stubSource);

        source.ShouldBeEmpty();
    }

    [Fact]
    public void UnchangedInput_ProducesByteIdenticalGeneratedSource()
    {
        var stubSource = """
using DotNetCore.Collections.Paginable;

namespace TestNamespace
{
    [SolidPageExtensionsFor(
        ormName: "TestOrm",
        sourceTypeFullName: "TestNs.ISource<T>",
        sourceParamName: "src",
        pageTypeName: "TestPage",
        paginableTypeName: "PaginableTest",
        factoryTypeName: "TestFactory",
        helperTypeName: "TestHelper")]
    public static partial class SolidPageExtensions { }
}

namespace TestNs
{
    public interface ISource<T> { }
}
""";
        var first = GeneratorHarness.Run(stubSource).generatedSource;
        var second = GeneratorHarness.Run(stubSource).generatedSource;

        first.ShouldNotBeEmpty();
        second.ShouldBe(first);
    }
}

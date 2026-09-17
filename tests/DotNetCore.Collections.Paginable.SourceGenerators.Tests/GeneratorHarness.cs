using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotNetCore.Collections.Paginable.SourceGenerators.Tests;

internal static class GeneratorHarness
{
    public static CSharpCompilation CreateCompilation(string stubSource)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(stubSource);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    public static GeneratorDriver CreateDriver()
        => CSharpGeneratorDriver.Create(new IIncrementalGenerator[] { new SolidPageExtensionsGenerator() });

    public static (string generatedSource, ImmutableArray<Diagnostic> diagnostics) Run(string stubSource)
    {
        var driver = CreateDriver().RunGenerators(CreateCompilation(stubSource));
        var generated = driver.GetRunResult().GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SolidPageExtensions.g.cs"));

        return (generated?.GetText().ToString() ?? "", driver.GetRunResult().Diagnostics);
    }

    public static string RepoRoot { get; } = FindRepoRoot();

    public static string StubPath(string package)
        => Path.Combine(RepoRoot, "src", $"DotNetCore.Collections.Paginable.{package}",
            "DotNetCore", "Collections", "Paginable", "Extensions", "SolidPageExtensions.cs");

    public static string ReadBaseline(string package)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "baselines", $"{package}.cs.baseline"));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DotNetCore.Collections.sln")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate DotNetCore.Collections.sln above the test assembly.");
    }
}

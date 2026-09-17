using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.SourceGenerators.Tests;

/// <summary>
/// Pins the F6-09 acceptance bar: for every migrated ORM adapter package, the members the generator
/// emits plus the members the package keeps handwritten must equal the pre-migration handwritten
/// SolidPageExtensions - signature, body and XML documentation, member for member.
/// </summary>
public class SolidPageExtensionsBaselineEquivalenceTests
{
    private static readonly string[] Packages =
    {
        "Chloe", "DosOrm", "FreeSql", "NHibernate", "SqlKata", "SqlSugar",
    };

    public static IEnumerable<object[]> PackageData => Packages.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(PackageData))]
    public void Members_match_the_handwritten_baseline(string package)
    {
        var actual = CurrentMembers(package);
        var expected = BaselineMembers(package);

        actual.Keys.OrderBy(k => k).ShouldBe(expected.Keys.OrderBy(k => k), $"{package}: public surface changed");

        foreach (var key in expected.Keys)
            actual[key].Code.ShouldBe(expected[key].Code, $"{package} :: {key}");
    }

    [Theory]
    [MemberData(nameof(PackageData))]
    public void Xml_docs_match_the_handwritten_baseline(string package)
    {
        var actual = CurrentMembers(package);
        var expected = BaselineMembers(package);

        foreach (var key in expected.Keys)
        {
            var baseline = expected[key];
            var blanks = BlankParams(baseline.DocLines);

            DocsOf(actual[key].DocLines, blanks).ShouldBe(DocsOf(baseline.DocLines, blanks), $"{package} :: {key} docs");
        }
    }

    [Theory]
    [MemberData(nameof(PackageData))]
    public void Every_generated_member_keeps_an_example_block(string package)
    {
        var generated = GeneratorHarness.Run(StubWithoutNet451Region(package)).generatedSource;

        var methods = Methods(generated).ToList();
        methods.ShouldNotBeEmpty();

        foreach (var method in methods)
            method.GetDocLines().ShouldContain("<example>", $"{package} :: {method.Identifier.Text} lost its example block");
    }

    private static Dictionary<string, Member> CurrentMembers(string package)
    {
        var stub = StubWithoutNet451Region(package);
        var generated = GeneratorHarness.Run(stub).generatedSource;
        generated.ShouldNotBeEmpty($"the generator produced nothing for {package}");

        var members = new Dictionary<string, Member>();
        Add(members, stub);
        Add(members, generated);
        return members;
    }

    private static Dictionary<string, Member> BaselineMembers(string package)
    {
        var members = new Dictionary<string, Member>();
        Add(members, GeneratorHarness.ReadBaseline(package));
        return members;
    }

    private static void Add(Dictionary<string, Member> target, string source)
    {
        foreach (var method in Methods(source))
        {
            var key = SignatureKey(method);
            target.ContainsKey(key).ShouldBe(false, $"duplicate member {key}");
            target[key] = new Member(NormalizeCode(method.ToString()), method.GetDocLines().ToArray());
        }
    }

    private static IEnumerable<MethodDeclarationSyntax> Methods(string source)
        => CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

    private static string SignatureKey(MethodDeclarationSyntax method)
        => $"{method.Identifier.Text}({string.Join(", ", method.ParameterList.Parameters
            .Select(p => NormalizeCode(p.Type.ToString()) + " " + p.Identifier.Text))})";

    private static string StubWithoutNet451Region(string package)
    {
        var path = GeneratorHarness.StubPath(package);
        File.Exists(path).ShouldBe(true, path);

        var kept = new List<string>();
        var skipped = new Stack<bool>();
        var depth = 0;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#if ", StringComparison.Ordinal))
            {
                var isNet451 = trimmed == "#if NET451";
                skipped.Push(isNet451);
                depth += isNet451 ? 1 : 0;
                continue;
            }

            if (trimmed.StartsWith("#endif", StringComparison.Ordinal))
            {
                depth -= skipped.Pop() ? 1 : 0;
                continue;
            }

            if (depth == 0)
                kept.Add(line);
        }

        return string.Join("\n", kept);
    }

    private static readonly Regex EmptyParam = new(@"^<param name=""(\w+)""></param>$", RegexOptions.Compiled);

    private static string[] BlankParams(IEnumerable<string> docLines)
        => docLines.Select(l => EmptyParam.Match(l))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToArray();

    /// <summary>
    /// Doc text with whitespace collapsed and the &lt;example&gt; block removed. Examples are excluded on
    /// purpose: the generator derives each one from the member it documents, which corrects the
    /// handwritten baseline's copy-paste drift. A parameter the baseline left undocumented is dropped on
    /// both sides, so filling that gap is not reported as a mismatch.
    /// </summary>
    private static string DocsOf(string[] docLines, string[] blankParams)
    {
        var kept = new List<string>();
        var inExample = false;

        foreach (var line in docLines)
        {
            if (line == "<example>")
            {
                inExample = true;
                continue;
            }

            if (line == "</example>")
            {
                inExample = false;
                continue;
            }

            if (inExample)
                continue;

            var name = EmptyParam.Match(line) is { Success: true } empty ? empty.Groups[1].Value : ParamName(line);
            if (name is null || !blankParams.Contains(name))
                kept.Add(line);
        }

        return NormalizeCode(string.Join(" ", kept));
    }

    private static string ParamName(string line)
    {
        const string tag = "<param name=\"";
        if (!line.StartsWith(tag, StringComparison.Ordinal)) return null;
        var end = line.IndexOf('"', tag.Length);
        return end < 0 ? null : line.Substring(tag.Length, end - tag.Length);
    }

    private static string NormalizeCode(string text)
    {
        text = text.Replace("global::", "");
        text = Regex.Replace(text, @"\bSystem\.Threading\.Tasks\.", "");
        text = Regex.Replace(text, @"\bSystem\.Threading\.", "");
        text = Regex.Replace(text, @"\bSystem\.(?=ArgumentNullException|ArgumentOutOfRangeException|Task)", "");
        text = Regex.Replace(text, @"(?<=[(,]\s)[\w@]+\s*:\s+(?=[\w@])", "");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private sealed record Member(string Code, string[] DocLines);
}

internal static class DocumentationCommentExtensions
{
    public static IEnumerable<string> GetDocLines(this MethodDeclarationSyntax method)
    {
        foreach (var trivia in method.GetLeadingTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                continue;

            foreach (var raw in trivia.ToString().Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("///", StringComparison.Ordinal))
                    yield return line.Substring(3).Trim();
            }
        }
    }
}

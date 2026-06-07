using System.Text.RegularExpressions;

namespace Soundtrail.Services.Tests.Architecture;

public sealed class FeatureFolderDependencyGuardTests
{
    private static readonly string[] AllowedUsingPrefixes =
    [
        "System",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions",
        "Soundtrail"
    ];

    private static readonly string[] BannedNamespacePrefixes =
    [
        "Raven",
        "Wolverine",
        "JasperFx",
        "Azure",
        "Dapper",
        "Npgsql",
        "MongoDB",
        "MassTransit",
        "Marten",
        "StackExchange.Redis",
        "Microsoft.Data.SqlClient",
        "System.Data.SqlClient",
        "Microsoft.EntityFrameworkCore"
    ];

    [Fact]
    public void Given_feature_folder_source_files_when_checking_imports_then_only_allowed_namespaces_are_used()
    {
        var violations = EnumerateFeatureFiles()
            .SelectMany(file => ReadUsingNamespaces(file)
                .Where(@using => AllowedUsingPrefixes.All(prefix => !IsWithin(@using, prefix)))
                .Select(@using => $"{ToRelativePath(file)} imports '{@using}'"))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Feature folders should only import System, approved Microsoft namespaces, or Soundtrail namespaces."
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Given_feature_folder_source_files_when_checking_references_then_infrastructure_packages_are_not_used()
    {
        var violations = EnumerateFeatureFiles()
            .SelectMany(file => FindBannedNamespaceReferences(file)
                .Select(match => $"{ToRelativePath(file)} references '{match}'"))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Feature folders should not depend on infrastructure packages directly."
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    private static string SolutionRoot =>
        FindAncestorContaining("Soundtrail.Services.sln")
        ?? throw new InvalidOperationException("Could not locate Soundtrail.Services.sln from the test output directory.");

    private static IEnumerable<string> EnumerateFeatureFiles()
    {
        var srcRoot = Path.Combine(SolutionRoot, "src");

        return Directory.EnumerateDirectories(srcRoot, "Features", SearchOption.AllDirectories)
            .SelectMany(featureDirectory => Directory.EnumerateFiles(featureDirectory, "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.Ordinal);
    }

    private static IEnumerable<string> ReadUsingNamespaces(string filePath)
    {
        var usingPattern = new Regex(@"^\s*(?:global\s+)?using\s+(?:static\s+)?(?<namespace>[^;]+);", RegexOptions.Multiline);
        var contents = File.ReadAllText(filePath);

        foreach (Match match in usingPattern.Matches(contents))
        {
            var candidate = match.Groups["namespace"].Value.Trim();

            if (candidate.Contains('='))
            {
                candidate = candidate[(candidate.IndexOf('=') + 1)..].Trim();
            }

            yield return candidate;
        }
    }

    private static IEnumerable<string> FindBannedNamespaceReferences(string filePath)
    {
        var contents = File.ReadAllText(filePath);

        foreach (var prefix in BannedNamespacePrefixes)
        {
            var pattern = $@"(?<![A-Za-z0-9_]){Regex.Escape(prefix)}(?:\.[A-Za-z0-9_]+)+";

            foreach (Match match in Regex.Matches(contents, pattern))
            {
                yield return match.Value;
            }
        }
    }

    private static bool IsWithin(string candidateNamespace, string prefix) =>
        candidateNamespace.Equals(prefix, StringComparison.Ordinal)
        || candidateNamespace.StartsWith(prefix + ".", StringComparison.Ordinal);

    private static string ToRelativePath(string absolutePath) =>
        Path.GetRelativePath(SolutionRoot, absolutePath);

    private static string? FindAncestorContaining(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, fileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

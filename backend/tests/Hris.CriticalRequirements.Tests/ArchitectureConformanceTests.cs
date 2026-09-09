using System.Runtime.CompilerServices;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-ARC-001 through CTR-ARC-004, docs/09-testing/critical-test-requirements.md §4,
/// with the required assertions spelled out in architecture-and-security-testing.md §2.2.
///
/// Implemented as Roslyn source-tree scans over <c>backend/src/</c> rather than as
/// assembly-reflection tests, following the precedent
/// <see cref="AuthorizationTests.CTR_AUT_001_NoRoleNameComparisonInCode"/> already sets
/// in this project. Two reasons, one practical and one structural.
///
/// The practical one: a source scan needs no <c>ProjectReference</c> to the fifteen
/// module and framework projects it inspects, so this file covers every module added in
/// a later Sprint automatically, with no edit here and no risk of a new module being
/// silently unverified because someone forgot to add a reference.
///
/// The structural one matters more. architecture-and-security-testing.md §2.2 phrases
/// CTR-ARC-001 as "the Domain <b>assembly</b> references only the Shared Kernel" and
/// CTR-ARC-003 as "each business entity is mapped in exactly one module's
/// <c>DbContext</c>". Neither shape exists in this codebase. The confirmed physical
/// layout is <b>one project per module</b> with <c>Domain/</c>, <c>Application/</c>, and
/// <c>Infrastructure/</c> as folders inside it, not separate assemblies (see STATUS.md's
/// own note on the two layout decisions confirmed before Phase 2 Sprint 1), and
/// persistence uses a single shared <c>HrisDbContext</c> that discovers configurations
/// through <c>PersistenceAssemblyRegistry</c> rather than one context per module. The
/// requirement's intent — dependency direction, and one owner per table — is fully
/// checkable; only its assumed granularity is not. These tests therefore assert the
/// intent at the granularity the architecture actually has: folder for CTR-ARC-001,
/// owning project for CTR-ARC-003.
/// </summary>
public class ArchitectureConformanceTests
{
    /// <summary>
    /// Namespaces a Domain-layer file may never import. The Clean Architecture
    /// dependency rule, and the one everything else in the architecture depends on:
    /// a Domain type that knows about EF Core cannot be constructed in a test without
    /// a provider, and a Domain type that knows about MediatR has an application
    /// concern embedded in a business rule.
    ///
    /// <c>Hris.SharedKernel</c> is permitted and is the only cross-cutting Hris
    /// namespace that is. <c>System.*</c> is permitted implicitly by absence from this
    /// list.
    /// </summary>
    private static readonly string[] _forbiddenInDomain =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions.DependencyInjection",
        "Npgsql",
        "MediatR",
        "FluentValidation",
        "Hris.Application",
        "Hris.Infrastructure",
    ];

    /// <summary>
    /// The one deviation from CTR-ARC-004 present in the codebase, recorded here
    /// deliberately rather than silently tolerated by a looser assertion.
    ///
    /// <c>Hris.Foundation.Audit.Domain.AuditRecord</c> is reached through
    /// <c>IAuditRecordRepository</c> but derives from <c>Entity&lt;AuditRecordId&gt;</c>
    /// rather than <c>AggregateRoot&lt;AuditRecordId&gt;</c>. Functionally it is a root:
    /// it is standalone, owns its own table, is never loaded through a parent, and has
    /// no aggregate above it. Structurally it is not, which is what this requirement
    /// keys off.
    ///
    /// The plausible justification, which that type's own remarks do not state, is that
    /// <c>AggregateRoot</c> exists to carry Domain Events and an audit record raises
    /// none — auditing is a terminal fact, not a trigger. That is defensible, but it
    /// leaves the base class no longer signalling what CTR-ARC-004 reads it for.
    ///
    /// Resolving this is an architecture decision about a Foundation framework, not one
    /// to make inside a test: either <c>AuditRecord</c> becomes an
    /// <c>AggregateRoot</c> and this entry disappears, or CTR-ARC-004's own text gains a
    /// stated exception for event-free standalone entities. Tracked in
    /// REVIEW-CODE-CONFORMANCE-2026-09-10.md. Until then this list must not grow: a
    /// second entry means the rule is being eroded rather than enforced.
    /// </summary>
    private static readonly string[] _knownRepositoryDeviations = ["AuditRecord"];

    /// <summary>
    /// CTR-ARC-001. Every file under a <c>Domain/</c> folder anywhere in
    /// <c>backend/src/</c> may import only the Shared Kernel, the BCL, and its own
    /// module's Domain namespace.
    /// </summary>
    [Fact]
    public void CTR_ARC_001_DomainLayerHasNoOutwardDependencies()
    {
        var violations = new List<string>();

        foreach (var filePath in EnumerateBackendSourceFiles().Where(IsDomainLayerFile))
        {
            foreach (var import in ParseUsings(filePath))
            {
                var forbidden = _forbiddenInDomain.FirstOrDefault(
                    prefix => import.Equals(prefix, StringComparison.Ordinal)
                              || import.StartsWith(prefix + ".", StringComparison.Ordinal));

                if (forbidden is not null)
                {
                    violations.Add($"{Relative(filePath)} imports {import}");
                }
            }
        }

        violations.Should().BeEmpty(
            "CTR-ARC-001 requires the Domain layer to depend outward on nothing but the Shared Kernel; "
            + "move the concern into Application or Infrastructure. Violations: " + string.Join("; ", violations));
    }

    /// <summary>
    /// CTR-ARC-002, asserted twice because the two checks fail differently. The
    /// <c>using</c> scan catches a reference someone wrote; the project-reference scan
    /// catches the thing that would have made it compile, and is the stronger of the
    /// two because it fails before any code is written against it.
    /// </summary>
    [Fact]
    public void CTR_ARC_002_NoCrossModuleInternalReferences()
    {
        var violations = new List<string>();

        foreach (var filePath in EnumerateBackendSourceFiles())
        {
            var owner = OwningModule(filePath);
            if (owner is null)
            {
                continue;
            }

            foreach (var import in ParseUsings(filePath).Where(u => u.StartsWith("Hris.Modules.", StringComparison.Ordinal)))
            {
                var referenced = import.Split('.')[2];
                if (!string.Equals(referenced, owner, StringComparison.Ordinal))
                {
                    violations.Add($"{Relative(filePath)} imports {import} (module {owner} referencing module {referenced})");
                }
            }
        }

        foreach (var projectPath in Directory.EnumerateFiles(ModulesDirectory(), "*.csproj", SearchOption.AllDirectories))
        {
            var owner = OwningModule(projectPath);

            // Parsed as XML rather than scanned as text on purpose. Every .csproj in
            // this repository carries a long explanatory comment, and several of them
            // name a sibling module's .csproj in prose precisely to explain why they do
            // NOT reference it -- Position's own header cites
            // Hris.Modules.Organization.csproj's remarks while declaring no such
            // reference. A substring scan reports those comments as violations, which
            // is the opposite of the truth.
            var references = XDocument.Load(projectPath)
                .Descendants()
                .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                .Select(e => e.Attribute("Include")?.Value)
                .OfType<string>()
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>();

            foreach (var reference in references.Where(r => r.StartsWith("Hris.Modules.", StringComparison.Ordinal)))
            {
                var referenced = reference.Replace("Hris.Modules.", string.Empty, StringComparison.Ordinal);
                if (!string.Equals(referenced, owner, StringComparison.Ordinal))
                {
                    violations.Add($"{Relative(projectPath)} declares a ProjectReference to {reference}");
                }
            }
        }

        violations.Should().BeEmpty(
            "CTR-ARC-002 and CTR-ARC-003 permit cross-module interaction only by identifier through a published "
            + "contract, never by referencing another module's types or project. Violations: " + string.Join("; ", violations));
    }

    /// <summary>
    /// CTR-ARC-003. Each table is configured by exactly one owning project. Shared
    /// tables are the most damaging form of coupling precisely because nothing in the
    /// type system signals them — two modules can map the same table indefinitely with
    /// no compile-time complaint, which is why this is asserted rather than reviewed.
    /// </summary>
    [Fact]
    public void CTR_ARC_003_NoSharedBusinessTables()
    {
        var owners = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var filePath in EnumerateBackendSourceFiles())
        {
            var project = OwningProject(filePath);
            if (project is null)
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath).GetRoot();

            var tableNames = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression is MemberAccessExpressionSyntax m
                            && string.Equals(m.Name.Identifier.ValueText, "ToTable", StringComparison.Ordinal))
                .Select(i => i.ArgumentList.Arguments.FirstOrDefault()?.Expression)
                .OfType<LiteralExpressionSyntax>()
                .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression))
                .Select(l => l.Token.ValueText);

            foreach (var table in tableNames)
            {
                if (!owners.TryGetValue(table, out var claimants))
                {
                    claimants = new SortedSet<string>(StringComparer.Ordinal);
                    owners[table] = claimants;
                }

                claimants.Add(project);
            }
        }

        owners.Should().NotBeEmpty("the scan must find mapped tables, or it is asserting nothing");

        var shared = owners.Where(entry => entry.Value.Count > 1)
            .Select(entry => $"table '{entry.Key}' mapped by {string.Join(" and ", entry.Value)}")
            .ToList();

        shared.Should().BeEmpty(
            "CTR-ARC-003 requires each business entity to be mapped in exactly one module. Violations: "
            + string.Join("; ", shared));
    }

    /// <summary>
    /// CTR-ARC-004. A repository over a child entity allows loading and modifying that
    /// child independently of its root, bypassing the invariants the root exists to
    /// protect — Administration's own separation-of-duties check is the worked example,
    /// since it can only be evaluated against a complete role set held inside one
    /// aggregate.
    ///
    /// The aggregate name is derived from the interface name by this codebase's own
    /// unbroken convention (<c>I{Aggregate}Repository</c>), which holds for all 51
    /// repository interfaces currently declared.
    /// </summary>
    [Fact]
    public void CTR_ARC_004_RepositoriesExistOnlyForAggregateRoots()
    {
        var aggregateRoots = new HashSet<string>(StringComparer.Ordinal);
        var repositoryTargets = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var filePath in EnumerateBackendSourceFiles())
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath).GetRoot();

            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var baseNames = type.BaseList?.Types
                    .Select(b => b.Type)
                    .OfType<GenericNameSyntax>()
                    .Select(g => g.Identifier.ValueText) ?? [];

                if (baseNames.Contains("AggregateRoot", StringComparer.Ordinal))
                {
                    aggregateRoots.Add(type.Identifier.ValueText);
                }

                var name = type.Identifier.ValueText;
                if (type is InterfaceDeclarationSyntax
                    && name.StartsWith('I')
                    && name.EndsWith("Repository", StringComparison.Ordinal))
                {
                    var target = name[1..^"Repository".Length];
                    repositoryTargets[target] = Relative(filePath);
                }
            }
        }

        aggregateRoots.Should().NotBeEmpty("the scan must find aggregate roots, or it is asserting nothing");
        repositoryTargets.Should().NotBeEmpty("the scan must find repository interfaces, or it is asserting nothing");

        var violations = repositoryTargets
            .Where(entry => !aggregateRoots.Contains(entry.Key))
            .Where(entry => !_knownRepositoryDeviations.Contains(entry.Key, StringComparer.Ordinal))
            .Select(entry => $"I{entry.Key}Repository ({entry.Value}) targets '{entry.Key}', which is not an AggregateRoot")
            .ToList();

        violations.Should().BeEmpty(
            "CTR-ARC-004 permits a repository only for an Aggregate Root. Violations: " + string.Join("; ", violations));
    }

    private static bool IsDomainLayerFile(string filePath) =>
        filePath.Contains($"{Path.DirectorySeparatorChar}Domain{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static IEnumerable<string> ParseUsings(string filePath) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath)
            .GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Alias is null && u.StaticKeyword.IsKind(SyntaxKind.None))
            .Select(u => u.Name?.ToString())
            .OfType<string>();

    /// <summary>Returns the module name for a path under <c>src/Modules/</c>, otherwise null.</summary>
    private static string? OwningModule(string filePath)
    {
        var segment = $"{Path.DirectorySeparatorChar}Modules{Path.DirectorySeparatorChar}Hris.Modules.";
        var index = filePath.IndexOf(segment, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var rest = filePath[(index + segment.Length)..];
        var end = rest.IndexOf(Path.DirectorySeparatorChar, StringComparison.Ordinal);
        return end < 0 ? rest : rest[..end];
    }

    /// <summary>Returns the owning project directory name for any file under <c>src/</c>.</summary>
    private static string? OwningProject(string filePath)
    {
        var srcDir = SourceDirectory();
        var relative = Path.GetRelativePath(srcDir, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar);

        // src/<area>/<project>/... — BuildingBlocks, Modules, and Api all follow this.
        return parts.Length >= 2 ? parts[1] : null;
    }

    private static string Relative(string filePath) => Path.GetRelativePath(SourceDirectory(), filePath);

    private static string ModulesDirectory() => Path.Combine(SourceDirectory(), "Modules");

    private static string SourceDirectory([CallerFilePath] string testFilePath = "")
    {
        // testFilePath = backend/tests/Hris.CriticalRequirements.Tests/ArchitectureConformanceTests.cs
        var testsProjectDir = Path.GetDirectoryName(testFilePath)!;
        var backendDir = Path.GetFullPath(Path.Combine(testsProjectDir, "..", ".."));
        return Path.Combine(backendDir, "src");
    }

    private static IEnumerable<string> EnumerateBackendSourceFiles()
    {
        var objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(SourceDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(objSegment, StringComparison.Ordinal)
                        && !f.Contains(binSegment, StringComparison.Ordinal));
    }
}

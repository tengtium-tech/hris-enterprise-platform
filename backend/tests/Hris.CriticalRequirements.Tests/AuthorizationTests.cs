using System.Runtime.CompilerServices;
using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-AUT-001 through CTR-AUT-010, docs/09-testing/critical-test-requirements.md §6.
/// CTR-AUT-001 specifically is enforced by a dedicated static-analysis rule per
/// coding-standards.md, not solely by a runtime test -- the test below still exists
/// as the runtime-behavior half of that requirement (a role-name comparison that
/// somehow reached runtime should still be caught here).
/// </summary>
public class AuthorizationTests
{
    /// <summary>
    /// Un-skipped now that Authorization Framework's <see cref="Role"/> exists to
    /// derive the prohibited literal set from -- the other nine CTR-AUT stubs below
    /// stay skipped because their own Verification text needs a business module's
    /// real endpoints, persisted data, or org-scope records that do not exist until
    /// Phase 2 onward; this one does not. Coding-standards.md's Static Analysis
    /// table calls the mechanism "a custom Roslyn analyzer or static-analysis rule";
    /// this test IS that rule, implemented as a source-tree scan (Roslyn syntax
    /// trees over every <c>.cs</c> file under backend/src/) rather than a build-time
    /// DiagnosticAnalyzer, so it needs no separate CI wiring beyond `dotnet test`
    /// already running in both pipelines -- and it covers every module added in a
    /// later Sprint automatically, with no change to this file, since it walks the
    /// whole src/ tree rather than naming projects.
    ///
    /// Flags an equality/inequality comparison where either side is a string literal
    /// exactly matching one of the ten canonical <see cref="Role"/> names -- the
    /// precise shape of authorization-framework.md's own prohibited example,
    /// <c>if (user.Role == "HR Manager")</c>. Enum-typed comparisons
    /// (<c>role == Role.HRManager</c>) and <c>Enum.TryParse&lt;Role&gt;</c> at a
    /// deserialization boundary are unaffected -- neither is a string literal
    /// comparison, and both are the correct way to move between a role name and
    /// <see cref="Role"/> itself.
    /// </summary>
    [Fact]
    public void CTR_AUT_001_NoRoleNameComparisonInCode()
    {
        var roleNames = Enum.GetNames<Role>().ToHashSet(StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var filePath in EnumerateBackendSourceFiles())
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath);

            var comparisons = tree.GetRoot()
                .DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .Where(b => b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression));

            foreach (var comparison in comparisons)
            {
                var literal = comparison.Left as LiteralExpressionSyntax ?? comparison.Right as LiteralExpressionSyntax;

                if (literal is not null
                    && literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && roleNames.Contains(literal.Token.ValueText))
                {
                    var line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    violations.Add($"{filePath}:{line} compares against role-name string literal \"{literal.Token.ValueText}\"");
                }
            }
        }

        violations.Should().BeEmpty(
            "CTR-AUT-001 / ADR-0002 prohibit comparing a role by name string anywhere in business logic; "
            + "route the decision through Hris.Foundation.Authorization.Domain.AuthorizationEvaluator instead. Violations: "
            + string.Join("; ", violations));
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-002 — Deny by Default.")]
    public void CTR_AUT_002_DenyByDefault()
    {
    }

    /// <summary>
    /// Un-skipped now that <see cref="RolePermissionGrant.Create"/> carries the
    /// structural guard this requirement needs. The CTR's own Verification text
    /// ("Test attempting every module's write operations as Auditor") describes a
    /// module-level, Application-layer test no business module can back yet
    /// (Phase 2 onward) -- what this test verifies instead is the one mechanism
    /// every future module's write operation will ultimately have to pass through
    /// to grant <see cref="Role.Auditor"/> a permission at all: the grant itself
    /// cannot be created. A grant that can never exist cannot later be exercised.
    /// </summary>
    [Theory]
    [InlineData(PermissionAction.Create)]
    [InlineData(PermissionAction.Update)]
    [InlineData(PermissionAction.Delete)]
    [InlineData(PermissionAction.Approve)]
    [InlineData(PermissionAction.Reject)]
    [InlineData(PermissionAction.Import)]
    [InlineData(PermissionAction.Configure)]
    public void CTR_AUT_003_AuditorHoldsNoMutationPermissions(PermissionAction mutatingAction)
    {
        var permission = PermissionKey.Create("EmployeeRecord", mutatingAction).Value;

        var result = RolePermissionGrant.Create(Role.Auditor, permission, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.AuditorCannotHoldMutationPermission);
    }

    /// <summary>
    /// The negative case: confirms the guard above is a genuine gate on mutation
    /// specifically, not an accidental blanket rejection of every
    /// <see cref="Role.Auditor"/> grant -- read access is exactly what DOC-012
    /// Section 7's Auditor persona exists to have.
    /// </summary>
    [Fact]
    public void CTR_AUT_003_AuditorCanStillHoldReadPermission()
    {
        var permission = PermissionKey.Create("EmployeeRecord", PermissionAction.Read).Value;

        var result = RolePermissionGrant.Create(Role.Auditor, permission, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-004 — Employee Access Limited to Own Records.")]
    public void CTR_AUT_004_EmployeeAccessLimitedToOwnRecords()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-005 — People Manager Access Limited to Reporting Line.")]
    public void CTR_AUT_005_PeopleManagerAccessLimitedToReportingLine()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-006 — Scope Is Enforced, Not Only Assigned.")]
    public void CTR_AUT_006_ScopeIsEnforcedNotOnlyAssigned()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-007 — Revocation Takes Effect Immediately.")]
    public void CTR_AUT_007_RevocationTakesEffectImmediately()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-008 — Deactivation Terminates Active Sessions.")]
    public void CTR_AUT_008_DeactivationTerminatesActiveSessions()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-009 — Permission Filtering Precedes Pagination.")]
    public void CTR_AUT_009_PermissionFilteringPrecedesPagination()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUT-010 — Aggregate/Reporting Access Does Not Grant Individual-Record Access.")]
    public void CTR_AUT_010_AggregateReportingAccessDoesNotGrantIndividualRecordAccess()
    {
    }

    private static IEnumerable<string> EnumerateBackendSourceFiles([CallerFilePath] string testFilePath = "")
    {
        // testFilePath = backend/tests/Hris.CriticalRequirements.Tests/AuthorizationTests.cs
        var testsProjectDir = Path.GetDirectoryName(testFilePath)!;
        var backendDir = Path.GetFullPath(Path.Combine(testsProjectDir, "..", ".."));
        var srcDir = Path.Combine(backendDir, "src");

        var objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(objSegment, StringComparison.Ordinal) && !f.Contains(binSegment, StringComparison.Ordinal));
    }
}

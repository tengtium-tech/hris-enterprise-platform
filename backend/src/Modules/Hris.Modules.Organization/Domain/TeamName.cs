using System.Text.RegularExpressions;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// The name of a <see cref="Team"/> (examples: "Backend Team", "Frontend Team",
/// "Payroll Team", "QA Team"). Source:
/// docs/04-modules/organization/domain/entities.md, Team. Uniqueness (TEAM-001's own
/// "Teams belong to a single Section" plus the platform's general per-parent
/// uniqueness convention) is enforced by <see cref="Organization.AddTeam"/> itself.
/// </summary>
public sealed partial class TeamName : ValueObject
{
    private const int _maxLength = 200;

    public string Value { get; }

    private TeamName(string value)
    {
        Value = value;
    }

    public static Result<TeamName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TeamName>(OrganizationErrors.TeamNameRequired);
        }

        var normalized = WhitespacePattern().Replace(value.Trim(), " ");

        return normalized.Length > _maxLength
            ? Result.Failure<TeamName>(OrganizationErrors.TeamNameRequired)
            : Result.Success(new TeamName(normalized));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}

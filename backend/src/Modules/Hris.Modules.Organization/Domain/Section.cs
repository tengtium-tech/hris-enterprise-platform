using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A subdivision of a <see cref="Department"/> (examples: "Recruitment",
/// "Compensation", "Employee Relations"), owning a bounded collection of
/// <see cref="Team"/> children. Source:
/// docs/04-modules/organization/domain/entities.md, Section. A child Entity, never
/// an Aggregate Root of its own; its constructor is <c>internal</c>, reachable only
/// through <see cref="Organization"/>. SEC-002 ("Archived Sections cannot receive
/// new Teams") is enforced by <see cref="Organization.AddTeam"/> checking this
/// Section's own <see cref="Status"/> before adding.
/// </summary>
public sealed class Section : Entity<SectionId>
{
    private readonly List<Team> _teams = [];

    public DepartmentId DepartmentId { get; }

    public SectionName Name { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<Team> Teams => _teams.AsReadOnly();

    internal Section(SectionId id, DepartmentId departmentId, SectionName name, DateTimeOffset createdAtUtc)
        : base(id)
    {
        DepartmentId = departmentId;
        Name = name;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Rename(SectionName newName)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        Name = newName;
        return Result.Success();
    }

    internal Result Archive()
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.AlreadyArchived);
        }

        Status = OrganizationalUnitStatus.Archived;
        return Result.Success();
    }

    internal Result<Team> AddTeam(TeamId id, TeamName name, DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure<Team>(OrganizationErrors.ParentArchived);
        }

        if (_teams.Any(t => t.Status != OrganizationalUnitStatus.Archived
            && t.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Team>(OrganizationErrors.DuplicateTeamName);
        }

        var team = new Team(id, Id, name, nowUtc);
        _teams.Add(team);
        return Result.Success(team);
    }

    internal Team? FindTeam(TeamId id) => _teams.SingleOrDefault(t => t.Id == id);
}

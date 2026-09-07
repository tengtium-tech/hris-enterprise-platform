using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A working team within a <see cref="Section"/> (examples: "Backend Team",
/// "Frontend Team", "QA Team"). Source:
/// docs/04-modules/organization/domain/entities.md, Team. The leaf of the
/// Organization Aggregate's own five-level nested hierarchy
/// (BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt; Team). A child
/// Entity, never an Aggregate Root of its own (aggregate-design-rules.md Rule 7):
/// its constructor is <c>internal</c>, reachable only through
/// <see cref="Organization"/>. Name uniqueness within the parent Section (TEAM-001)
/// is enforced by <see cref="Organization.AddTeam"/>, which already has every
/// sibling Team loaded.
/// </summary>
public sealed class Team : Entity<TeamId>
{
    public SectionId SectionId { get; }

    public TeamName Name { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal Team(TeamId id, SectionId sectionId, TeamName name, DateTimeOffset createdAtUtc)
        : base(id)
    {
        SectionId = sectionId;
        Name = name;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Rename(TeamName newName)
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
}

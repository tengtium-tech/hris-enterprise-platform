using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// An accounting or budgeting unit directly beneath an <see cref="Organization"/>
/// (examples: "IT Operations", "Sales Region A", "Corporate Services"). Source:
/// docs/04-modules/organization/domain/entities.md, CostCenter. Sits alongside the
/// BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt; Team chain rather
/// than inside it, per aggregates.md's own Aggregate Landscape diagram, which lists
/// Cost Centers as a direct sibling of Business Units under Organization. A child
/// Entity, never an Aggregate Root of its own; its constructor is <c>internal</c>,
/// reachable only through <see cref="Organization"/>. Code uniqueness (CC-001,
/// tenant-wide) is checked by the Application layer against the repository, not by
/// this Entity or by <see cref="Organization"/> itself.
/// </summary>
public sealed class CostCenter : Entity<CostCenterId>
{
    public CostCenterCode Code { get; }

    public string? Description { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal CostCenter(CostCenterId id, CostCenterCode code, string? description, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Code = code;
        Description = description;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Update(string? description)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        Description = description;
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

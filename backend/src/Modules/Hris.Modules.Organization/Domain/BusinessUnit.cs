using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A major business grouping directly beneath an <see cref="Organization"/>
/// (examples: "Corporate", "Manufacturing", "Sales"), owning a bounded collection of
/// <see cref="Division"/> children. Source:
/// docs/04-modules/organization/domain/entities.md, BusinessUnit. A child Entity,
/// never an Aggregate Root of its own; its constructor is <c>internal</c>, reachable
/// only through <see cref="Organization"/>. BU-002 ("Archived Business Units cannot
/// receive new Divisions") is enforced by <see cref="AddDivision"/> checking this
/// unit's own <see cref="Status"/>.
/// </summary>
public sealed class BusinessUnit : Entity<BusinessUnitId>
{
    private readonly List<Division> _divisions = [];

    public BusinessUnitName Name { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<Division> Divisions => _divisions.AsReadOnly();

    internal BusinessUnit(BusinessUnitId id, BusinessUnitName name, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Rename(BusinessUnitName newName)
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

    internal Result Restore()
    {
        if (Status != OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.NotArchived);
        }

        Status = OrganizationalUnitStatus.Active;
        return Result.Success();
    }

    internal Result<Division> AddDivision(DivisionId id, DivisionName name, DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure<Division>(OrganizationErrors.ParentArchived);
        }

        if (_divisions.Any(d => d.Status != OrganizationalUnitStatus.Archived
            && d.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Division>(OrganizationErrors.DuplicateDivisionName);
        }

        var division = new Division(id, Id, name, nowUtc);
        _divisions.Add(division);
        return Result.Success(division);
    }

    internal Division? FindDivision(DivisionId id) => _divisions.SingleOrDefault(d => d.Id == id);
}

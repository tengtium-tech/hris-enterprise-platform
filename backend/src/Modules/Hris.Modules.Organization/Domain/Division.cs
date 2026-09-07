using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// A subdivision of a <see cref="BusinessUnit"/> (examples: "Software Division",
/// "Hardware Division", "Consumer Products"), owning a bounded collection of
/// <see cref="Department"/> children. Source:
/// docs/04-modules/organization/domain/entities.md, Division. A child Entity, never
/// an Aggregate Root of its own; its constructor is <c>internal</c>, reachable only
/// through <see cref="Organization"/>. DEPT-002's own tenant-wide Department code
/// uniqueness is checked by the Application layer, not here; this Entity only
/// enforces DEPT-001's own narrower "within this Division" name uniqueness in
/// <see cref="AddDepartment"/>.
/// </summary>
public sealed class Division : Entity<DivisionId>
{
    private readonly List<Department> _departments = [];

    public BusinessUnitId BusinessUnitId { get; private set; }

    public DivisionName Name { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<Department> Departments => _departments.AsReadOnly();

    internal Division(DivisionId id, BusinessUnitId businessUnitId, DivisionName name, DateTimeOffset createdAtUtc)
        : base(id)
    {
        BusinessUnitId = businessUnitId;
        Name = name;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Rename(DivisionName newName)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        Name = newName;
        return Result.Success();
    }

    internal Result MoveTo(BusinessUnitId newBusinessUnitId)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        BusinessUnitId = newBusinessUnitId;
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

    internal Result<Department> AddDepartment(
        DepartmentId id, DepartmentName name, DepartmentCode code, DateTimeOffset nowUtc,
        DepartmentId? splitFromDepartmentId = null)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure<Department>(OrganizationErrors.ParentArchived);
        }

        if (_departments.Any(d => d.Status != OrganizationalUnitStatus.Archived
            && d.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Department>(OrganizationErrors.DuplicateDepartmentName);
        }

        var department = new Department(id, Id, name, code, nowUtc, splitFromDepartmentId);
        _departments.Add(department);
        return Result.Success(department);
    }

    internal Department? FindDepartment(DepartmentId id) => _departments.SingleOrDefault(d => d.Id == id);
}

using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// An operational department (examples: "Human Resources", "Finance", "Information
/// Technology"), owning a bounded collection of <see cref="Section"/> children.
/// Source: docs/04-modules/organization/domain/entities.md, Department. A child
/// Entity, never an Aggregate Root of its own; its constructor is <c>internal</c>,
/// reachable only through <see cref="Organization"/>.
///
/// <see cref="MergedIntoDepartmentId"/> and <see cref="SplitFromDepartmentId"/> exist
/// for DEPT-006 ("Department merges must preserve historical references") and the
/// symmetric split case: a merged-away or split-from Department is archived, not
/// deleted, and keeps a pointer to where its records now live so historical employee
/// and audit references stay traceable (business-rules.md, DATA-001).
///
/// DEPT-004 ("A Department with active employees cannot be archived") is
/// deliberately not checked here or anywhere in this Aggregate: the Employee module
/// does not exist yet (IMPLEMENTATION-PLAN.md, Phase 2 Sprint 4), so nothing in this
/// codebase can currently answer "does this Department have active employees."
/// Enforcing DEPT-004 belongs to that future Sprint's own Application layer, once a
/// concrete Employee repository exists to ask. DEPT-007's own "must not orphan
/// employees" on split carries the identical gap.
/// </summary>
public sealed class Department : Entity<DepartmentId>
{
    private readonly List<Section> _sections = [];

    public DivisionId DivisionId { get; private set; }

    public DepartmentName Name { get; private set; }

    public DepartmentCode Code { get; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DepartmentId? MergedIntoDepartmentId { get; private set; }

    public DepartmentId? SplitFromDepartmentId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<Section> Sections => _sections.AsReadOnly();

    internal Department(
        DepartmentId id, DivisionId divisionId, DepartmentName name, DepartmentCode code, DateTimeOffset createdAtUtc,
        DepartmentId? splitFromDepartmentId = null)
        : base(id)
    {
        DivisionId = divisionId;
        Name = name;
        Code = code;
        Status = OrganizationalUnitStatus.Active;
        SplitFromDepartmentId = splitFromDepartmentId;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Rename(DepartmentName newName)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        Name = newName;
        return Result.Success();
    }

    internal Result MoveTo(DivisionId newDivisionId)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        DivisionId = newDivisionId;
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

    internal Result MarkMergedInto(DepartmentId survivingDepartmentId)
    {
        MergedIntoDepartmentId = survivingDepartmentId;
        return Archive();
    }

    internal Result<Section> AddSection(SectionId id, SectionName name, DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure<Section>(OrganizationErrors.ParentArchived);
        }

        if (_sections.Any(s => s.Status != OrganizationalUnitStatus.Archived
            && s.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Section>(OrganizationErrors.DuplicateSectionName);
        }

        var section = new Section(id, Id, name, nowUtc);
        _sections.Add(section);
        return Result.Success(section);
    }

    internal Section? FindSection(SectionId id) => _sections.SingleOrDefault(s => s.Id == id);
}

using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Aggregate Root of the enterprise organizational hierarchy. Source:
/// docs/04-modules/organization/domain/aggregates.md and entities.md, whose own
/// Summary sections both state this module is built around exactly three Aggregate
/// Roots (Organization, LegalEntity, WorkLocation), not the eight items
/// module-overview.md's own looser Aggregate Overview lists -- aggregates.md's own
/// design principle ("minimizes the number of Aggregate Roots... grouping closely
/// related concepts that change together") explicitly consolidates BusinessUnit,
/// Division, Department, Section, Team, and CostCenter into this Aggregate as child
/// Entities.
///
/// The hierarchy nests exactly one level per type
/// (BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt; Team), per
/// entities.md's own Entity Relationships diagram, with CostCenter as a direct
/// sibling of BusinessUnit rather than nested beneath it. Because each level is a
/// distinct C# type rather than a self-referencing tree, a Move operation
/// (<see cref="MoveDivision"/>, <see cref="MoveDepartment"/>) can only reassign a
/// unit to a different parent of the correct type one level up -- there is no way
/// for that reassignment to create a cycle, since a unit's own descendants are
/// always of a *different*, strictly lower-level type than any valid new parent.
/// HIER-001's acyclic requirement and DIV-002/DEPT-003's own "cannot be moved
/// beneath one of its own descendants" wording therefore describe a failure mode
/// this type shape already makes structurally unreachable, per this project's own
/// "prefer structure over discipline" convention -- no runtime graph-traversal check
/// is needed to prevent something the compiler-enforced type nesting already
/// prevents. <see cref="OrganizationErrors.InvalidHierarchyMove"/> is retained for
/// the one case that *is* still possible: the caller-supplied target parent id not
/// existing on this Aggregate at all.
///
/// <see cref="LegalEntityId"/> is a plain, optional, caller-supplied <see cref="Guid"/>
/// to the independent <see cref="LegalEntity"/> Aggregate Root, per this platform's
/// own standing rule that no Sprint 4+ framework or Phase 2+ module takes a
/// compile-time reference to a sibling Aggregate's own type, confirmed here against
/// infrastructure/persistence.md's own Aggregate References section ("Organization
/// references LegalEntityId... Aggregate instances should never directly reference
/// other Aggregate instances").
///
/// Every public method below takes raw primitives (<c>string?</c>), not pre-built
/// Value Objects, and constructs the corresponding Value Object internally before
/// using it, exactly the convention <c>Document.Create</c> and
/// <c>Connector.Register</c> already establish elsewhere in this codebase: it keeps
/// this Aggregate's own public surface free of CA1062 ("validate reference-type
/// parameters before dereferencing them"), and it means an Application-layer command
/// handler passes DTO fields straight through without first constructing a Value
/// Object itself.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "\"Organization\" is this module's own binding ubiquitous-language term "
        + "(docs/04-modules/organization/), the same class of justification SharedKernel's own "
        + "Error type already carries for CA1716. Renaming the Aggregate Root to avoid colliding "
        + "with its own module's namespace segment would depart from the documented vocabulary "
        + "for no benefit, and every later Phase 2-6 module will have exactly this shape (a "
        + "module named after its own primary Aggregate).")]
public sealed class Organization : AggregateRoot<OrganizationId>
{
    private readonly List<BusinessUnit> _businessUnits = [];
    private readonly List<CostCenter> _costCenters = [];

    public Guid TenantId { get; }

    public OrganizationName Name { get; private set; }

    public OrganizationCode Code { get; }

    public Guid? LegalEntityId { get; private set; }

    public string? Description { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<BusinessUnit> BusinessUnits => _businessUnits.AsReadOnly();

    public IReadOnlyList<CostCenter> CostCenters => _costCenters.AsReadOnly();

    private Organization(
        OrganizationId id, Guid tenantId, OrganizationName name, OrganizationCode code, Guid? legalEntityId,
        string? description, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        LegalEntityId = legalEntityId;
        Description = description;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Organization> Create(
        OrganizationId id, Guid tenantId, string? name, string? code, Guid? legalEntityId, string? description,
        DateTimeOffset nowUtc)
    {
        var nameResult = OrganizationName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<Organization>(nameResult.Error);
        }

        var codeResult = OrganizationCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<Organization>(codeResult.Error);
        }

        var organization = new Organization(id, tenantId, nameResult.Value, codeResult.Value, legalEntityId, description, nowUtc);
        organization.AddDomainEvent(
            new OrganizationCreated(Guid.NewGuid(), nowUtc, id, tenantId, codeResult.Value.Value, nameResult.Value.Value));
        return Result.Success(organization);
    }

    public Result Rename(string? newName, DateTimeOffset nowUtc)
    {
        var nameResult = OrganizationName.Create(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        Name = nameResult.Value;
        AddDomainEvent(new OrganizationRenamed(Guid.NewGuid(), nowUtc, Id, nameResult.Value.Value));
        return Result.Success();
    }

    public Result Update(Guid? legalEntityId, string? description, DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        LegalEntityId = legalEntityId;
        Description = description;
        AddDomainEvent(new OrganizationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.AlreadyArchived);
        }

        if (_businessUnits.Any(bu => bu.Status == OrganizationalUnitStatus.Active)
            || _costCenters.Any(cc => cc.Status == OrganizationalUnitStatus.Active))
        {
            return Result.Failure(OrganizationErrors.CannotArchiveWithActiveChildren);
        }

        Status = OrganizationalUnitStatus.Archived;
        AddDomainEvent(new OrganizationArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Restore(DateTimeOffset nowUtc)
    {
        if (Status != OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.NotArchived);
        }

        Status = OrganizationalUnitStatus.Active;
        AddDomainEvent(new OrganizationRestored(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    // Business Unit management.

    public Result<BusinessUnitId> AddBusinessUnit(string? name, DateTimeOffset nowUtc)
    {
        var nameResult = BusinessUnitName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<BusinessUnitId>(nameResult.Error);
        }

        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure<BusinessUnitId>(OrganizationErrors.ParentArchived);
        }

        if (_businessUnits.Any(bu => bu.Status != OrganizationalUnitStatus.Archived
            && bu.Name.Value.Equals(nameResult.Value.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<BusinessUnitId>(OrganizationErrors.DuplicateBusinessUnitName);
        }

        var businessUnit = new BusinessUnit(new BusinessUnitId(Guid.NewGuid()), nameResult.Value, nowUtc);
        _businessUnits.Add(businessUnit);
        AddDomainEvent(new BusinessUnitCreated(Guid.NewGuid(), nowUtc, Id, businessUnit.Id, nameResult.Value.Value));
        return Result.Success(businessUnit.Id);
    }

    public Result RenameBusinessUnit(BusinessUnitId businessUnitId, string? newName, DateTimeOffset nowUtc)
    {
        var nameResult = BusinessUnitName.Create(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var businessUnit = FindBusinessUnit(businessUnitId);
        if (businessUnit is null)
        {
            return Result.Failure(OrganizationErrors.BusinessUnitNotFound);
        }

        var result = businessUnit.Rename(nameResult.Value);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new BusinessUnitRenamed(Guid.NewGuid(), nowUtc, Id, businessUnitId));
        return Result.Success();
    }

    public Result ArchiveBusinessUnit(BusinessUnitId businessUnitId, DateTimeOffset nowUtc)
    {
        var businessUnit = FindBusinessUnit(businessUnitId);
        if (businessUnit is null)
        {
            return Result.Failure(OrganizationErrors.BusinessUnitNotFound);
        }

        var result = businessUnit.Archive();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new BusinessUnitArchived(Guid.NewGuid(), nowUtc, Id, businessUnitId));
        return Result.Success();
    }

    public Result RestoreBusinessUnit(BusinessUnitId businessUnitId)
    {
        var businessUnit = FindBusinessUnit(businessUnitId);
        return businessUnit is null
            ? Result.Failure(OrganizationErrors.BusinessUnitNotFound)
            : businessUnit.Restore();
    }

    // Division management.

    public Result<DivisionId> AddDivision(BusinessUnitId businessUnitId, string? name, DateTimeOffset nowUtc)
    {
        var nameResult = DivisionName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<DivisionId>(nameResult.Error);
        }

        var businessUnit = FindBusinessUnit(businessUnitId);
        if (businessUnit is null)
        {
            return Result.Failure<DivisionId>(OrganizationErrors.BusinessUnitNotFound);
        }

        var result = businessUnit.AddDivision(new DivisionId(Guid.NewGuid()), nameResult.Value, nowUtc);
        if (result.IsFailure)
        {
            return Result.Failure<DivisionId>(result.Error);
        }

        AddDomainEvent(new DivisionCreated(Guid.NewGuid(), nowUtc, Id, result.Value.Id, businessUnitId, nameResult.Value.Value));
        return Result.Success(result.Value.Id);
    }

    public Result RenameDivision(DivisionId divisionId, string? newName, DateTimeOffset nowUtc)
    {
        var nameResult = DivisionName.Create(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var division = FindDivision(divisionId);
        if (division is null)
        {
            return Result.Failure(OrganizationErrors.DivisionNotFound);
        }

        var result = division.Rename(nameResult.Value);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DivisionRenamed(Guid.NewGuid(), nowUtc, Id, divisionId));
        return Result.Success();
    }

    public Result MoveDivision(DivisionId divisionId, BusinessUnitId newBusinessUnitId, DateTimeOffset nowUtc)
    {
        var division = FindDivision(divisionId);
        if (division is null)
        {
            return Result.Failure(OrganizationErrors.DivisionNotFound);
        }

        if (FindBusinessUnit(newBusinessUnitId) is null)
        {
            return Result.Failure(OrganizationErrors.InvalidHierarchyMove);
        }

        var result = division.MoveTo(newBusinessUnitId);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DivisionMoved(Guid.NewGuid(), nowUtc, Id, divisionId, newBusinessUnitId));
        return Result.Success();
    }

    public Result ArchiveDivision(DivisionId divisionId, DateTimeOffset nowUtc)
    {
        var division = FindDivision(divisionId);
        if (division is null)
        {
            return Result.Failure(OrganizationErrors.DivisionNotFound);
        }

        var result = division.Archive();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DivisionArchived(Guid.NewGuid(), nowUtc, Id, divisionId));
        return Result.Success();
    }

    // Department management.

    public Result<DepartmentId> AddDepartment(DivisionId divisionId, string? name, string? code, DateTimeOffset nowUtc)
    {
        var nameResult = DepartmentName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<DepartmentId>(nameResult.Error);
        }

        var codeResult = DepartmentCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<DepartmentId>(codeResult.Error);
        }

        var division = FindDivision(divisionId);
        if (division is null)
        {
            return Result.Failure<DepartmentId>(OrganizationErrors.DivisionNotFound);
        }

        var result = division.AddDepartment(new DepartmentId(Guid.NewGuid()), nameResult.Value, codeResult.Value, nowUtc);
        if (result.IsFailure)
        {
            return Result.Failure<DepartmentId>(result.Error);
        }

        AddDomainEvent(new DepartmentCreated(
            Guid.NewGuid(), nowUtc, Id, result.Value.Id, divisionId, nameResult.Value.Value, codeResult.Value.Value));
        return Result.Success(result.Value.Id);
    }

    public Result RenameDepartment(DepartmentId departmentId, string? newName, DateTimeOffset nowUtc)
    {
        var nameResult = DepartmentName.Create(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var department = FindDepartment(departmentId);
        if (department is null)
        {
            return Result.Failure(OrganizationErrors.DepartmentNotFound);
        }

        var result = department.Rename(nameResult.Value);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DepartmentRenamed(Guid.NewGuid(), nowUtc, Id, departmentId));
        return Result.Success();
    }

    public Result MoveDepartment(DepartmentId departmentId, DivisionId newDivisionId, DateTimeOffset nowUtc)
    {
        var department = FindDepartment(departmentId);
        if (department is null)
        {
            return Result.Failure(OrganizationErrors.DepartmentNotFound);
        }

        if (FindDivision(newDivisionId) is null)
        {
            return Result.Failure(OrganizationErrors.InvalidHierarchyMove);
        }

        var result = department.MoveTo(newDivisionId);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DepartmentMoved(Guid.NewGuid(), nowUtc, Id, departmentId, newDivisionId));
        return Result.Success();
    }

    /// <summary>
    /// DEPT-005 ("A Department cannot be merged across different Organizations") is
    /// automatically satisfied: every source id is resolved against this Aggregate
    /// instance alone, so a department belonging to another Organization can never
    /// be found here in the first place. The surviving Department is created fresh
    /// under the first source's own Division (source Divisions need not all match;
    /// the surviving record picks one explicitly rather than guessing which source's
    /// parent should win), and every source is archived with
    /// <see cref="Department.MergedIntoDepartmentId"/> set for DEPT-006's own
    /// traceability requirement.
    /// </summary>
    public Result<DepartmentId> MergeDepartments(
        IReadOnlyCollection<DepartmentId> sourceDepartmentIds, string? survivingName, string? survivingCode,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(sourceDepartmentIds);

        var nameResult = DepartmentName.Create(survivingName);
        if (nameResult.IsFailure)
        {
            return Result.Failure<DepartmentId>(nameResult.Error);
        }

        var codeResult = DepartmentCode.Create(survivingCode);
        if (codeResult.IsFailure)
        {
            return Result.Failure<DepartmentId>(codeResult.Error);
        }

        if (sourceDepartmentIds.Count < 2)
        {
            return Result.Failure<DepartmentId>(OrganizationErrors.DepartmentMergeRequiresAtLeastTwoSources);
        }

        var sources = new List<Department>();
        foreach (var sourceId in sourceDepartmentIds)
        {
            var source = FindDepartment(sourceId);
            if (source is null)
            {
                return Result.Failure<DepartmentId>(OrganizationErrors.DepartmentNotFound);
            }

            sources.Add(source);
        }

        var survivingDivisionId = sources[0].DivisionId;
        var division = FindDivision(survivingDivisionId)!;
        var createResult = division.AddDepartment(new DepartmentId(Guid.NewGuid()), nameResult.Value, codeResult.Value, nowUtc);
        if (createResult.IsFailure)
        {
            return Result.Failure<DepartmentId>(createResult.Error);
        }

        var survivor = createResult.Value;
        foreach (var source in sources)
        {
            source.MarkMergedInto(survivor.Id);
        }

        AddDomainEvent(new DepartmentMerged(
            Guid.NewGuid(), nowUtc, Id, survivor.Id, sources.Select(s => s.Id).ToList()));
        return Result.Success(survivor.Id);
    }

    /// <summary>
    /// DEPT-007's own "must not orphan employees" cannot be enforced here: this
    /// Aggregate has no visibility into the Employee module, which does not exist
    /// yet (IMPLEMENTATION-PLAN.md, Phase 2 Sprint 4). Reassigning any employees
    /// pointed at <paramref name="sourceDepartmentId"/> is that future Sprint's own
    /// Application-layer responsibility, coordinated after this call returns, the
    /// same documented-gap pattern <see cref="OrganizationErrors"/>'s own remarks
    /// state for DEPT-004.
    /// </summary>
    public Result<IReadOnlyList<DepartmentId>> SplitDepartment(
        DepartmentId sourceDepartmentId, IReadOnlyCollection<(string? Name, string? Code)> newDepartments,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(newDepartments);

        if (newDepartments.Count < 2)
        {
            return Result.Failure<IReadOnlyList<DepartmentId>>(OrganizationErrors.DepartmentSplitRequiresAtLeastTwoTargets);
        }

        var source = FindDepartment(sourceDepartmentId);
        if (source is null)
        {
            return Result.Failure<IReadOnlyList<DepartmentId>>(OrganizationErrors.DepartmentNotFound);
        }

        var division = FindDivision(source.DivisionId)!;
        var created = new List<DepartmentId>();
        foreach (var (name, code) in newDepartments)
        {
            var nameResult = DepartmentName.Create(name);
            if (nameResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<DepartmentId>>(nameResult.Error);
            }

            var codeResult = DepartmentCode.Create(code);
            if (codeResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<DepartmentId>>(codeResult.Error);
            }

            var result = division.AddDepartment(
                new DepartmentId(Guid.NewGuid()), nameResult.Value, codeResult.Value, nowUtc, sourceDepartmentId);
            if (result.IsFailure)
            {
                return Result.Failure<IReadOnlyList<DepartmentId>>(result.Error);
            }

            created.Add(result.Value.Id);
        }

        source.Archive();
        AddDomainEvent(new DepartmentSplit(Guid.NewGuid(), nowUtc, Id, sourceDepartmentId, created));
        return Result.Success<IReadOnlyList<DepartmentId>>(created);
    }

    public Result ArchiveDepartment(DepartmentId departmentId, DateTimeOffset nowUtc)
    {
        var department = FindDepartment(departmentId);
        if (department is null)
        {
            return Result.Failure(OrganizationErrors.DepartmentNotFound);
        }

        var result = department.Archive();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DepartmentArchived(Guid.NewGuid(), nowUtc, Id, departmentId));
        return Result.Success();
    }

    public Result RestoreDepartment(DepartmentId departmentId, DateTimeOffset nowUtc)
    {
        var department = FindDepartment(departmentId);
        if (department is null)
        {
            return Result.Failure(OrganizationErrors.DepartmentNotFound);
        }

        var result = department.Restore();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new DepartmentRestored(Guid.NewGuid(), nowUtc, Id, departmentId));
        return Result.Success();
    }

    // Section management.

    public Result<SectionId> AddSection(DepartmentId departmentId, string? name, DateTimeOffset nowUtc)
    {
        var nameResult = SectionName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<SectionId>(nameResult.Error);
        }

        var department = FindDepartment(departmentId);
        if (department is null)
        {
            return Result.Failure<SectionId>(OrganizationErrors.DepartmentNotFound);
        }

        var result = department.AddSection(new SectionId(Guid.NewGuid()), nameResult.Value, nowUtc);
        if (result.IsFailure)
        {
            return Result.Failure<SectionId>(result.Error);
        }

        AddDomainEvent(new SectionCreated(Guid.NewGuid(), nowUtc, Id, result.Value.Id, departmentId, nameResult.Value.Value));
        return Result.Success(result.Value.Id);
    }

    public Result RenameSection(SectionId sectionId, string? newName, DateTimeOffset nowUtc)
    {
        var nameResult = SectionName.Create(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var section = FindSection(sectionId);
        if (section is null)
        {
            return Result.Failure(OrganizationErrors.SectionNotFound);
        }

        var result = section.Rename(nameResult.Value);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new SectionRenamed(Guid.NewGuid(), nowUtc, Id, sectionId));
        return Result.Success();
    }

    public Result ArchiveSection(SectionId sectionId, DateTimeOffset nowUtc)
    {
        var section = FindSection(sectionId);
        if (section is null)
        {
            return Result.Failure(OrganizationErrors.SectionNotFound);
        }

        var result = section.Archive();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new SectionArchived(Guid.NewGuid(), nowUtc, Id, sectionId));
        return Result.Success();
    }

    // Team management.

    public Result<TeamId> AddTeam(SectionId sectionId, string? name, DateTimeOffset nowUtc)
    {
        var nameResult = TeamName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<TeamId>(nameResult.Error);
        }

        var section = FindSection(sectionId);
        if (section is null)
        {
            return Result.Failure<TeamId>(OrganizationErrors.SectionNotFound);
        }

        var result = section.AddTeam(new TeamId(Guid.NewGuid()), nameResult.Value, nowUtc);
        if (result.IsFailure)
        {
            return Result.Failure<TeamId>(result.Error);
        }

        AddDomainEvent(new TeamCreated(Guid.NewGuid(), nowUtc, Id, result.Value.Id, sectionId, nameResult.Value.Value));
        return Result.Success(result.Value.Id);
    }

    public Result RenameTeam(TeamId teamId, string? newName, DateTimeOffset nowUtc)
    {
        var nameResult = TeamName.Create(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var team = FindTeam(teamId);
        if (team is null)
        {
            return Result.Failure(OrganizationErrors.TeamNotFound);
        }

        var result = team.Rename(nameResult.Value);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new TeamRenamed(Guid.NewGuid(), nowUtc, Id, teamId));
        return Result.Success();
    }

    public Result ArchiveTeam(TeamId teamId, DateTimeOffset nowUtc)
    {
        var team = FindTeam(teamId);
        if (team is null)
        {
            return Result.Failure(OrganizationErrors.TeamNotFound);
        }

        var result = team.Archive();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new TeamArchived(Guid.NewGuid(), nowUtc, Id, teamId));
        return Result.Success();
    }

    // Cost Center management.

    public Result<CostCenterId> AddCostCenter(string? code, string? description, DateTimeOffset nowUtc)
    {
        var codeResult = CostCenterCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<CostCenterId>(codeResult.Error);
        }

        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure<CostCenterId>(OrganizationErrors.ParentArchived);
        }

        var costCenter = new CostCenter(new CostCenterId(Guid.NewGuid()), codeResult.Value, description, nowUtc);
        _costCenters.Add(costCenter);
        AddDomainEvent(new CostCenterCreated(Guid.NewGuid(), nowUtc, Id, costCenter.Id, codeResult.Value.Value));
        return Result.Success(costCenter.Id);
    }

    public Result UpdateCostCenter(CostCenterId costCenterId, string? description, DateTimeOffset nowUtc)
    {
        var costCenter = FindCostCenter(costCenterId);
        if (costCenter is null)
        {
            return Result.Failure(OrganizationErrors.CostCenterNotFound);
        }

        var result = costCenter.Update(description);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new CostCenterUpdated(Guid.NewGuid(), nowUtc, Id, costCenterId));
        return Result.Success();
    }

    public Result ArchiveCostCenter(CostCenterId costCenterId, DateTimeOffset nowUtc)
    {
        var costCenter = FindCostCenter(costCenterId);
        if (costCenter is null)
        {
            return Result.Failure(OrganizationErrors.CostCenterNotFound);
        }

        var result = costCenter.Archive();
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new CostCenterArchived(Guid.NewGuid(), nowUtc, Id, costCenterId));
        return Result.Success();
    }

    // Traversal helpers.

    public BusinessUnit? FindBusinessUnit(BusinessUnitId id) => _businessUnits.SingleOrDefault(bu => bu.Id == id);

    public Division? FindDivision(DivisionId id) => _businessUnits.Select(bu => bu.FindDivision(id)).FirstOrDefault(d => d is not null);

    public Department? FindDepartment(DepartmentId id) =>
        _businessUnits.SelectMany(bu => bu.Divisions).Select(d => d.FindDepartment(id)).FirstOrDefault(dep => dep is not null);

    public Section? FindSection(SectionId id) =>
        _businessUnits.SelectMany(bu => bu.Divisions).SelectMany(d => d.Departments)
            .Select(dep => dep.FindSection(id)).FirstOrDefault(s => s is not null);

    public Team? FindTeam(TeamId id) =>
        _businessUnits.SelectMany(bu => bu.Divisions).SelectMany(d => d.Departments).SelectMany(dep => dep.Sections)
            .Select(s => s.FindTeam(id)).FirstOrDefault(t => t is not null);

    public CostCenter? FindCostCenter(CostCenterId id) => _costCenters.SingleOrDefault(cc => cc.Id == id);
}

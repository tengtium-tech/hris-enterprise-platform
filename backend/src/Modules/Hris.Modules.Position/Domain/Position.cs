using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Aggregate Root representing an authorized organizational role, independent of
/// any employee who may occupy it. Source: docs/04-modules/position/domain/aggregates.md
/// and entities.md, both of which name this as the module's own primary Aggregate
/// Root; docs/04-modules/position/domain/position-profile.md's own Business
/// Definition ("A Position is an approved organizational role... may exist whether
/// or not it is currently occupied").
///
/// Every organizational reference (<see cref="OrganizationId"/> through
/// <see cref="CostCenterId"/>), every workforce classification reference
/// (<see cref="JobFamilyId"/>, <see cref="JobClassificationId"/>,
/// <see cref="JobGradeId"/>), and <see cref="ReportingPositionId"/> is a plain,
/// caller-supplied <see cref="Guid"/>, never a compile-time reference to another
/// Aggregate's own type -- this platform's own standing "reference by identifier,
/// never by instance" rule, identical to <see cref="Guid"/>
/// <c>WorkLocation.OrganizationId</c> already applies for a cross-aggregate
/// reference within the same module. <see cref="ReportingPositionId"/> is notable
/// among these: it references another instance of this SAME Aggregate type, which
/// is why cycle prevention (position-hierarchy.md: "Circular references are
/// prohibited") cannot be a structural type-hierarchy guarantee the way it is for
/// Organization's own owned-entity tree -- no single loaded Position Aggregate can
/// see the rest of the reporting graph. <see cref="AssignReportingPosition"/>
/// therefore accepts three pre-computed booleans
/// (<paramref name="reportingPositionExists"/>-shaped, see that method's own
/// remarks) that the Application layer's command handler computes by querying
/// <c>IPositionRepository</c> before calling this method -- the aggregate still owns
/// the business decision, but the cross-aggregate graph traversal that decision
/// depends on is necessarily an Infrastructure-layer concern (repositories.md: "Not
/// responsible for business rules... orchestration").
///
/// "Position" as both the module's own root namespace segment and this class's own
/// name reproduces the exact CS0118 name-resolution collision found while building
/// the Organization module (see feedback memory
/// <c>feedback-module-namespace-collision</c>): any reference to this type from
/// outside its own Domain namespace must be fully qualified as
/// <c>Hris.Modules.Position.Domain.Position</c>, never the short <c>Domain.Position</c>
/// partial form.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "\"Position\" is this module's own binding ubiquitous-language term "
        + "(docs/04-modules/position/) -- the identical precedent Hris.Modules.Organization.Domain.Organization "
        + "already sets for its own module, and SharedKernel.Error's own CA1716 suppression sets before that. "
        + "Renaming the Aggregate Root to avoid a namespace collision would depart from the documented business "
        + "vocabulary for no benefit.")]
public sealed class Position : AggregateRoot<PositionId>
{
    public Guid TenantId { get; }

    public PositionNumber Number { get; }

    public PositionTitle Title { get; private set; }

    public PositionType PositionType { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? LegalEntityId { get; private set; }

    public Guid? BusinessUnitId { get; private set; }

    public Guid? DivisionId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public Guid? SectionId { get; private set; }

    public Guid? TeamId { get; private set; }

    public Guid? WorkLocationId { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public Guid JobFamilyId { get; private set; }

    public Guid JobClassificationId { get; private set; }

    public Guid JobGradeId { get; private set; }

    public Guid? ReportingPositionId { get; private set; }

    public AuthorizedHeadcount AuthorizedHeadcount { get; private set; }

    public PositionStatus Status { get; private set; }

    public VacancyStatus VacancyStatus { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    private Position(
        PositionId id, Guid tenantId, PositionNumber number, PositionTitle title, PositionType positionType,
        Guid organizationId, Guid? legalEntityId, Guid? businessUnitId, Guid? divisionId, Guid? departmentId,
        Guid? sectionId, Guid? teamId, Guid? workLocationId, Guid? costCenterId, Guid jobFamilyId,
        Guid jobClassificationId, Guid jobGradeId, Guid? reportingPositionId, AuthorizedHeadcount authorizedHeadcount,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Number = number;
        Title = title;
        PositionType = positionType;
        OrganizationId = organizationId;
        LegalEntityId = legalEntityId;
        BusinessUnitId = businessUnitId;
        DivisionId = divisionId;
        DepartmentId = departmentId;
        SectionId = sectionId;
        TeamId = teamId;
        WorkLocationId = workLocationId;
        CostCenterId = costCenterId;
        JobFamilyId = jobFamilyId;
        JobClassificationId = jobClassificationId;
        JobGradeId = jobGradeId;
        ReportingPositionId = reportingPositionId;
        AuthorizedHeadcount = authorizedHeadcount;
        Status = PositionStatus.Draft;
        VacancyStatus = VacancyStatus.Vacant;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Position> Create(
        PositionId id, Guid tenantId, string? number, string? title, string? positionType, Guid organizationId,
        Guid? legalEntityId, Guid? businessUnitId, Guid? divisionId, Guid? departmentId, Guid? sectionId,
        Guid? teamId, Guid? workLocationId, Guid? costCenterId, Guid jobFamilyId, Guid jobClassificationId,
        Guid jobGradeId, Guid? reportingPositionId, int authorizedHeadcount, DateTimeOffset nowUtc)
    {
        var numberResult = PositionNumber.Create(number);
        if (numberResult.IsFailure)
        {
            return Result.Failure<Position>(numberResult.Error);
        }

        var titleResult = PositionTitle.Create(title);
        if (titleResult.IsFailure)
        {
            return Result.Failure<Position>(titleResult.Error);
        }

        var positionTypeResult = PositionType.Create(positionType);
        if (positionTypeResult.IsFailure)
        {
            return Result.Failure<Position>(positionTypeResult.Error);
        }

        if (reportingPositionId == id.Value)
        {
            return Result.Failure<Position>(PositionErrors.SelfReportingProhibited);
        }

        var headcountResult = AuthorizedHeadcount.Create(authorizedHeadcount);
        if (headcountResult.IsFailure)
        {
            return Result.Failure<Position>(headcountResult.Error);
        }

        var position = new Position(
            id, tenantId, numberResult.Value, titleResult.Value, positionTypeResult.Value, organizationId,
            legalEntityId, businessUnitId, divisionId, departmentId, sectionId, teamId, workLocationId, costCenterId,
            jobFamilyId, jobClassificationId, jobGradeId, reportingPositionId, headcountResult.Value, nowUtc);
        position.AddDomainEvent(new PositionCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, numberResult.Value.Value, titleResult.Value.Value, organizationId));
        return Result.Success(position);
    }

    public Result Update(
        string? title, string? positionType, Guid organizationId, Guid? legalEntityId, Guid? businessUnitId,
        Guid? divisionId, Guid? departmentId, Guid? sectionId, Guid? teamId, Guid? workLocationId,
        Guid? costCenterId, Guid jobFamilyId, Guid jobClassificationId, Guid jobGradeId, DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        var titleResult = PositionTitle.Create(title);
        if (titleResult.IsFailure)
        {
            return Result.Failure(titleResult.Error);
        }

        var positionTypeResult = PositionType.Create(positionType);
        if (positionTypeResult.IsFailure)
        {
            return Result.Failure(positionTypeResult.Error);
        }

        Title = titleResult.Value;
        PositionType = positionTypeResult.Value;
        OrganizationId = organizationId;
        LegalEntityId = legalEntityId;
        BusinessUnitId = businessUnitId;
        DivisionId = divisionId;
        DepartmentId = departmentId;
        SectionId = sectionId;
        TeamId = teamId;
        WorkLocationId = workLocationId;
        CostCenterId = costCenterId;
        JobFamilyId = jobFamilyId;
        JobClassificationId = jobClassificationId;
        JobGradeId = jobGradeId;
        AddDomainEvent(new PositionUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (Status == PositionStatus.Active)
        {
            return Result.Failure(PositionErrors.PositionAlreadyActive);
        }

        Status = PositionStatus.Active;
        AddDomainEvent(new PositionActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Deactivate(DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (Status != PositionStatus.Active)
        {
            return Result.Failure(PositionErrors.PositionNotActive);
        }

        Status = PositionStatus.Inactive;
        AddDomainEvent(new PositionDeactivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.AlreadyArchived);
        }

        if (Status == PositionStatus.Draft)
        {
            return Result.Failure(PositionErrors.PositionCannotArchiveDraft);
        }

        Status = PositionStatus.Archived;
        AddDomainEvent(new PositionArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Assigns or changes this Position's own reporting position.
    /// <paramref name="reportingPositionExists"/>, <paramref name="reportingPositionIsActive"/>,
    /// and <paramref name="wouldCreateCircularReporting"/> are computed by the
    /// calling Application-layer command handler against <c>IPositionRepository</c>
    /// before this method is called -- see this class's own remarks for why an
    /// Aggregate cannot compute them itself.
    /// </summary>
    public Result AssignReportingPosition(
        Guid reportingPositionId, bool reportingPositionExists, bool reportingPositionIsActive,
        bool wouldCreateCircularReporting, DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (reportingPositionId == Id.Value)
        {
            return Result.Failure(PositionErrors.SelfReportingProhibited);
        }

        if (!reportingPositionExists)
        {
            return Result.Failure(PositionErrors.ReportingPositionNotFound);
        }

        if (!reportingPositionIsActive)
        {
            return Result.Failure(PositionErrors.ReportingPositionMustBeActive);
        }

        if (wouldCreateCircularReporting)
        {
            return Result.Failure(PositionErrors.CircularReportingProhibited);
        }

        var isChange = ReportingPositionId.HasValue;
        ReportingPositionId = reportingPositionId;
        AddDomainEvent(
            isChange
                ? new ReportingPositionChanged(Guid.NewGuid(), nowUtc, Id, reportingPositionId)
                : new ReportingPositionAssigned(Guid.NewGuid(), nowUtc, Id, reportingPositionId));
        return Result.Success();
    }

    public Result RemoveReportingPosition(DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (!ReportingPositionId.HasValue)
        {
            return Result.Failure(PositionErrors.NoReportingPositionToRemove);
        }

        ReportingPositionId = null;
        AddDomainEvent(new ReportingPositionRemoved(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result UpdateAuthorizedHeadcount(int newAuthorizedHeadcount, DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        var headcountResult = AuthorizedHeadcount.Create(newAuthorizedHeadcount);
        if (headcountResult.IsFailure)
        {
            return Result.Failure(headcountResult.Error);
        }

        AuthorizedHeadcount = headcountResult.Value;
        AddDomainEvent(new AuthorizedHeadcountChanged(Guid.NewGuid(), nowUtc, Id, headcountResult.Value.Value));
        return Result.Success();
    }

    public Result MarkVacant(DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (VacancyStatus == VacancyStatus.Vacant)
        {
            return Result.Failure(PositionErrors.PositionAlreadyVacant);
        }

        VacancyStatus = VacancyStatus.Vacant;
        AddDomainEvent(new PositionMarkedVacant(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result MarkFilled(DateTimeOffset nowUtc)
    {
        if (Status == PositionStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (VacancyStatus == VacancyStatus.Filled)
        {
            return Result.Failure(PositionErrors.PositionAlreadyFilled);
        }

        VacancyStatus = VacancyStatus.Filled;
        AddDomainEvent(new PositionFilled(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Aggregate Root representing an organizational grading level (examples: "Grade 1",
/// "Senior Professional", "Director"). Source:
/// docs/04-modules/position/domain/aggregates.md and entities.md, both of which name
/// this as one of the module's own four Aggregate Roots. <see cref="OrganizationalLevel"/>
/// is an optional numeric rank (job-grades.md's own Business Characteristics: "Grade
/// Code, Grade Name, Description, Organizational Level, Operational Status") used
/// only for ordering/comparison; this module deliberately does not calculate
/// compensation from it (job-grades.md: "The Position Module does not calculate
/// payroll or compensation").
/// </summary>
public sealed class JobGrade : AggregateRoot<JobGradeId>
{
    public Guid TenantId { get; }

    public JobGradeCode Code { get; }

    public JobGradeName Name { get; private set; }

    public string? Description { get; private set; }

    public int? OrganizationalLevel { get; private set; }

    public WorkforceClassificationStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    private JobGrade(
        JobGradeId id, Guid tenantId, JobGradeCode code, JobGradeName name, string? description,
        int? organizationalLevel, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Description = description;
        OrganizationalLevel = organizationalLevel;
        Status = WorkforceClassificationStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<JobGrade> Create(
        JobGradeId id, Guid tenantId, string? code, string? name, string? description, int? organizationalLevel,
        DateTimeOffset nowUtc)
    {
        var codeResult = JobGradeCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<JobGrade>(codeResult.Error);
        }

        var nameResult = JobGradeName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<JobGrade>(nameResult.Error);
        }

        var jobGrade = new JobGrade(id, tenantId, codeResult.Value, nameResult.Value, description, organizationalLevel, nowUtc);
        jobGrade.AddDomainEvent(
            new JobGradeCreated(Guid.NewGuid(), nowUtc, id, tenantId, codeResult.Value.Value, nameResult.Value.Value));
        return Result.Success(jobGrade);
    }

    public Result Update(string? name, string? description, int? organizationalLevel, DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        var nameResult = JobGradeName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        Name = nameResult.Value;
        Description = description;
        OrganizationalLevel = organizationalLevel;
        AddDomainEvent(new JobGradeUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (Status == WorkforceClassificationStatus.Active)
        {
            return Result.Failure(PositionErrors.WorkforceClassificationAlreadyActive);
        }

        Status = WorkforceClassificationStatus.Active;
        AddDomainEvent(new JobGradeActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Deactivate(DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        if (Status == WorkforceClassificationStatus.Inactive)
        {
            return Result.Failure(PositionErrors.WorkforceClassificationAlreadyInactive);
        }

        Status = WorkforceClassificationStatus.Inactive;
        AddDomainEvent(new JobGradeDeactivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.AlreadyArchived);
        }

        Status = WorkforceClassificationStatus.Archived;
        AddDomainEvent(new JobGradeArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Aggregate Root representing a standardized category of work (examples:
/// "Executive", "Professional", "Technical", "Administrative"). Source:
/// docs/04-modules/position/domain/aggregates.md and entities.md, both of which name
/// this as one of the module's own four Aggregate Roots. Organizationally
/// independent (job-classifications.md: "Job Classifications are independent of...
/// Departments, Business Units, Locations, Legal Entities, Employees").
/// </summary>
public sealed class JobClassification : AggregateRoot<JobClassificationId>
{
    public Guid TenantId { get; }

    public JobClassificationCode Code { get; }

    public JobClassificationName Name { get; private set; }

    public string? Description { get; private set; }

    public WorkforceClassificationStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    private JobClassification(
        JobClassificationId id, Guid tenantId, JobClassificationCode code, JobClassificationName name,
        string? description, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Description = description;
        Status = WorkforceClassificationStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<JobClassification> Create(
        JobClassificationId id, Guid tenantId, string? code, string? name, string? description, DateTimeOffset nowUtc)
    {
        var codeResult = JobClassificationCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<JobClassification>(codeResult.Error);
        }

        var nameResult = JobClassificationName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<JobClassification>(nameResult.Error);
        }

        var jobClassification = new JobClassification(id, tenantId, codeResult.Value, nameResult.Value, description, nowUtc);
        jobClassification.AddDomainEvent(new JobClassificationCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, codeResult.Value.Value, nameResult.Value.Value));
        return Result.Success(jobClassification);
    }

    public Result Update(string? name, string? description, DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        var nameResult = JobClassificationName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        Name = nameResult.Value;
        Description = description;
        AddDomainEvent(new JobClassificationUpdated(Guid.NewGuid(), nowUtc, Id));
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
        AddDomainEvent(new JobClassificationActivated(Guid.NewGuid(), nowUtc, Id));
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
        AddDomainEvent(new JobClassificationDeactivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.AlreadyArchived);
        }

        Status = WorkforceClassificationStatus.Archived;
        AddDomainEvent(new JobClassificationArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

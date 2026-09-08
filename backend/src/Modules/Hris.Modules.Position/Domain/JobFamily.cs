using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// Aggregate Root representing a group of Positions performing similar work
/// (examples: "Information Technology", "Human Resources", "Finance"). Source:
/// docs/04-modules/position/domain/aggregates.md and entities.md, both of which name
/// this as one of the module's own four Aggregate Roots. Organizationally
/// independent (job-families.md: "The same Job Family may exist across... Legal
/// Entities, Business Units, Divisions, Departments, Locations") -- carries no
/// organizational reference of any kind, unlike <see cref="Position"/> itself.
/// </summary>
public sealed class JobFamily : AggregateRoot<JobFamilyId>
{
    public Guid TenantId { get; }

    public JobFamilyCode Code { get; }

    public JobFamilyName Name { get; private set; }

    public string? Description { get; private set; }

    public WorkforceClassificationStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    private JobFamily(
        JobFamilyId id, Guid tenantId, JobFamilyCode code, JobFamilyName name, string? description,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Description = description;
        Status = WorkforceClassificationStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<JobFamily> Create(
        JobFamilyId id, Guid tenantId, string? code, string? name, string? description, DateTimeOffset nowUtc)
    {
        var codeResult = JobFamilyCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<JobFamily>(codeResult.Error);
        }

        var nameResult = JobFamilyName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<JobFamily>(nameResult.Error);
        }

        var jobFamily = new JobFamily(id, tenantId, codeResult.Value, nameResult.Value, description, nowUtc);
        jobFamily.AddDomainEvent(
            new JobFamilyCreated(Guid.NewGuid(), nowUtc, id, tenantId, codeResult.Value.Value, nameResult.Value.Value));
        return Result.Success(jobFamily);
    }

    public Result Update(string? name, string? description, DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.ArchivedCannotBeModified);
        }

        var nameResult = JobFamilyName.Create(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        Name = nameResult.Value;
        Description = description;
        AddDomainEvent(new JobFamilyUpdated(Guid.NewGuid(), nowUtc, Id));
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
        AddDomainEvent(new JobFamilyActivated(Guid.NewGuid(), nowUtc, Id));
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
        AddDomainEvent(new JobFamilyDeactivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == WorkforceClassificationStatus.Archived)
        {
            return Result.Failure(PositionErrors.AlreadyArchived);
        }

        Status = WorkforceClassificationStatus.Archived;
        AddDomainEvent(new JobFamilyArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

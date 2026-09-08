using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Aggregate Root managing the formal, legally relevant agreement governing an
/// Employment -- the legal artifact, never the relationship itself. Source:
/// docs/04-modules/employment/domain/aggregates.md and entities.md, and ADR-0008's
/// own "Why the Contract Is Not the Relationship" ("a contract ending does not
/// necessarily end the employment... one employment may run through several
/// successive contracts"). References <see cref="EmploymentId"/> by plain
/// <see cref="Guid"/> only, identical to <c>Guid WorkLocation.OrganizationId</c>'s
/// own established precedent for a cross-aggregate reference within the same
/// module (Position.cs's own remarks) -- <see cref="Employment"/> and
/// <see cref="EmploymentContract"/> never hold instance references to one another.
/// </summary>
public sealed class EmploymentContract : AggregateRoot<EmploymentContractId>
{
    private readonly List<ContractRenewal> _renewals = [];
    private readonly List<ContractExtension> _extensions = [];
    private readonly List<ContractDocument> _documents = [];

    public Guid TenantId { get; }

    public Guid EmploymentId { get; }

    public ContractType ContractType { get; private set; }

    // Assigned via object-initializer in Create(), never through the constructor:
    // ContractPeriod is mapped as an OwnsOne navigation (two columns: start date,
    // end date), and EF Core's ConstructorBindingConvention refuses to bind any
    // constructor parameter to a navigation property (see CompensationRecord.Amount's
    // own remarks and memory feedback-ef-core-constructor-binding).
    public ContractPeriod Period { get; private set; } = null!;

    public ContractLifecycleStage LifecycleStage { get; private set; }

    /// <summary>
    /// The prior Employment Contract this one replaced on a type-changing renewal
    /// (for example, Probationary superseded by Regular on regularization), if any.
    /// Populated by the caller when creating the successor contract; this Aggregate
    /// does not reach into the superseded contract to set its own Superseded stage --
    /// the Application-layer command handler calls <see cref="Supersede"/> on that
    /// separate instance (ADR-0008's own Contract Lifecycle table).
    /// </summary>
    public Guid? SupersedesContractId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<ContractRenewal> Renewals => _renewals.AsReadOnly();

    public IReadOnlyList<ContractExtension> Extensions => _extensions.AsReadOnly();

    public IReadOnlyList<ContractDocument> Documents => _documents.AsReadOnly();

    private EmploymentContract(
        EmploymentContractId id, Guid tenantId, Guid employmentId, ContractType contractType,
        Guid? supersedesContractId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        EmploymentId = employmentId;
        ContractType = contractType;
        LifecycleStage = ContractLifecycleStage.Draft;
        SupersedesContractId = supersedesContractId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates a new Employment Contract in Draft stage. <paramref name="isFixedTerm"/>
    /// is caller-supplied (the command/UI layer knows whether the chosen Contract
    /// Type is fixed-term) and enforces CON-005's "fixed-term Contracts must have a
    /// defined end date".
    /// </summary>
    public static Result<EmploymentContract> Create(
        EmploymentContractId id, Guid tenantId, Guid employmentId, string? contractType, DateOnly startDate,
        DateOnly? endDate, bool isFixedTerm, Guid? supersedesContractId, DateTimeOffset nowUtc)
    {
        var typeResult = ContractType.Create(contractType);
        if (typeResult.IsFailure)
        {
            return Result.Failure<EmploymentContract>(typeResult.Error);
        }

        if (isFixedTerm && !endDate.HasValue)
        {
            return Result.Failure<EmploymentContract>(EmploymentErrors.FixedTermContractRequiresEndDate);
        }

        var periodResult = ContractPeriod.Create(startDate, endDate);
        if (periodResult.IsFailure)
        {
            return Result.Failure<EmploymentContract>(periodResult.Error);
        }

        var contract = new EmploymentContract(id, tenantId, employmentId, typeResult.Value, supersedesContractId, nowUtc)
        {
            Period = periodResult.Value,
        };
        contract.AddDomainEvent(new EmploymentContractCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, employmentId, startDate, endDate));
        return Result.Success(contract);
    }

    public Result Approve(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Draft)
        {
            return Result.Failure(EmploymentErrors.ContractNotDraft);
        }

        LifecycleStage = ContractLifecycleStage.Approved;
        AddDomainEvent(new EmploymentContractApproved(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result MakeEffective(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Approved)
        {
            return Result.Failure(EmploymentErrors.ContractNotApproved);
        }

        LifecycleStage = ContractLifecycleStage.Effective;
        AddDomainEvent(new EmploymentContractEffective(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Extends the employment relationship by establishing a new Contract Period
    /// following the current one, within the same Contract instance -- the
    /// same-Contract-Type renewal case. A type-changing renewal is instead modeled
    /// as this Contract transitioning to <see cref="Supersede"/> while a new
    /// <see cref="EmploymentContract"/> instance is created referencing this one
    /// through <see cref="SupersedesContractId"/> (ADR-0008).
    /// </summary>
    public Result Renew(DateOnly newStartDate, DateOnly? newEndDate, string? approvalReference, DateTimeOffset nowUtc)
    {
        if (LifecycleStage == ContractLifecycleStage.Closed || LifecycleStage == ContractLifecycleStage.Cancelled
            || LifecycleStage == ContractLifecycleStage.Superseded)
        {
            return Result.Failure(EmploymentErrors.ContractCannotRenewAfterClosed);
        }

        var newPeriodResult = ContractPeriod.Create(newStartDate, newEndDate);
        if (newPeriodResult.IsFailure)
        {
            return Result.Failure(newPeriodResult.Error);
        }

        var renewal = new ContractRenewal(
            new ContractRenewalId(Guid.NewGuid()), Period.StartDate, Period.EndDate, newStartDate, newEndDate,
            approvalReference, nowUtc);
        _renewals.Add(renewal);
        Period = newPeriodResult.Value;
        LifecycleStage = ContractLifecycleStage.Effective;

        AddDomainEvent(new EmploymentContractRenewed(Guid.NewGuid(), nowUtc, Id, renewal.Id, newStartDate, newEndDate));
        return Result.Success();
    }

    public Result Extend(DateOnly newEndDate, string? reason, DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Effective)
        {
            return Result.Failure(EmploymentErrors.ContractNotEffective);
        }

        if (Period.EndDate.HasValue && newEndDate < Period.EndDate.Value)
        {
            return Result.Failure(EmploymentErrors.ContractPeriodEndBeforeStart);
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        var previousEndDate = Period.EndDate ?? Period.StartDate;
        var extension = new ContractExtension(
            new ContractExtensionId(Guid.NewGuid()), previousEndDate, newEndDate, normalizedReason, nowUtc);
        _extensions.Add(extension);

        var newPeriodResult = ContractPeriod.Create(Period.StartDate, newEndDate);
        if (newPeriodResult.IsFailure)
        {
            return Result.Failure(newPeriodResult.Error);
        }

        Period = newPeriodResult.Value;
        AddDomainEvent(new EmploymentContractExtended(Guid.NewGuid(), nowUtc, Id, extension.Id, newEndDate));
        return Result.Success();
    }

    public Result Expire(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Effective)
        {
            return Result.Failure(EmploymentErrors.ContractNotEffective);
        }

        LifecycleStage = ContractLifecycleStage.Expired;
        AddDomainEvent(new EmploymentContractExpired(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Supersede(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Effective && LifecycleStage != ContractLifecycleStage.Approved)
        {
            return Result.Failure(EmploymentErrors.ContractNotEffective);
        }

        LifecycleStage = ContractLifecycleStage.Superseded;
        AddDomainEvent(new EmploymentContractSuperseded(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Close(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Effective)
        {
            return Result.Failure(EmploymentErrors.ContractNotEffective);
        }

        LifecycleStage = ContractLifecycleStage.Closed;
        AddDomainEvent(new EmploymentContractClosed(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != ContractLifecycleStage.Draft && LifecycleStage != ContractLifecycleStage.Approved)
        {
            return Result.Failure(EmploymentErrors.ContractNotDraftOrApproved);
        }

        LifecycleStage = ContractLifecycleStage.Cancelled;
        AddDomainEvent(new EmploymentContractCancelled(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result AddDocument(string? documentType, string? storageReference, int version, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(storageReference))
        {
            return Result.Failure(EmploymentErrors.ContractTypeRequired);
        }

        _documents.Add(new ContractDocument(
            new ContractDocumentId(Guid.NewGuid()), documentType.Trim(), storageReference.Trim(), version, nowUtc));
        return Result.Success();
    }
}

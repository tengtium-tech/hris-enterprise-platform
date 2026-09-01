using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// One immutable record of who changed what, when, and from where. Source:
/// docs/03-foundation/audit-framework.md, Audit Record Structure.
///
/// Structurally implements `CTR-AUD-001` ("No application path may update or delete
/// an audit record"): every property below is <c>get</c>-only, assigned once in the
/// constructor, and this class declares no method that mutates an existing instance
/// -- there is no <c>Update</c>, no <c>Correct</c>, no setter, not even an
/// <c>internal</c> one a same-assembly caller could reach. Immutability here is a
/// property of the type itself, checkable by reading its public surface, not a
/// convention every future caller has to remember (this project's own engineering
/// principle: "Prefer structure over discipline"). An <see cref="Entity{TId}"/> base is used rather than
/// a plain <c>record</c> (contrast <c>LogEntry</c>) because <see cref="Id"/> is an
/// explicit "Audit Identifier" this document calls out, and two separate audit facts
/// that happen to carry identical field values are still two distinct records --
/// identity equality is the more correct fit here.
///
/// <see cref="PreviousValue"/> and <see cref="NewValue"/> are opaque, already-
/// serialized strings (a JSON snapshot, typically) rather than richly typed business
/// data: this framework audits change facts about every current and future business
/// entity (Employee, Payroll, Configuration, and so on), and giving Domain layer
/// knowledge of every one of those shapes would invert the dependency this
/// framework's own Upstream Dependencies list states (Identity, Authorization,
/// Event, Configuration -- never a business module). Serialization happens at
/// whatever call site already holds the strongly typed before/after state, before
/// <see cref="Create"/> is called.
///
/// <b>This is not business history.</b> audit-framework.md's own "Audit Is Not
/// History" section is explicit that retroactive computation reads history, which
/// each business module owns and writes transactionally with its own change
/// (`CTR-AUD-003`, `CTR-AUD-004`, `CTR-PAY-004`) -- never this framework. An
/// implementation that reconstructed point-in-time business state by replaying
/// <see cref="AuditRecord"/>s would be "either wrong or unusably slow," in that
/// section's own words; this type exists for accountability and compliance, not for
/// business queries.
/// </summary>
public sealed class AuditRecord : Entity<AuditRecordId>
{
    public DateTimeOffset TimestampUtc { get; }

    public UserAccountId? ActorId { get; }

    public AuditCategory Category { get; }

    public string Action { get; }

    public string BusinessEntity { get; }

    public string EntityIdentifier { get; }

    public string? PreviousValue { get; }

    public string? NewValue { get; }

    public string SourceSystem { get; }

    public string? ClientApplication { get; }

    public string? IpAddress { get; }

    public string? DeviceInformation { get; }

    public CorrelationId? CorrelationId { get; }

    public AuditResult Outcome { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    private AuditRecord(
        AuditRecordId id,
        DateTimeOffset timestampUtc,
        UserAccountId? actorId,
        AuditCategory category,
        string action,
        string businessEntity,
        string entityIdentifier,
        string? previousValue,
        string? newValue,
        string sourceSystem,
        string? clientApplication,
        string? ipAddress,
        string? deviceInformation,
        CorrelationId? correlationId,
        // Named to match the mapped property (Outcome), not Create's own `result`
        // parameter -- EF Core's constructor-binding convention matches a constructor
        // parameter to a property by name, and a parameter named `result` has no
        // `Result` property to bind to (AuditRecordConfiguration maps `Outcome`, per
        // this class's own AuditResult-is-an-enum shape, no owned-type navigation
        // involved). Unlike this file's four sibling fixes elsewhere in this Sprint
        // (ConfigurationSetting.Scope and similar), this one is a plain rename, not an
        // additive second constructor -- the parameter was always scalar-bindable,
        // just misnamed.
        AuditResult outcome,
        IReadOnlyDictionary<string, string> metadata)
        : base(id)
    {
        TimestampUtc = timestampUtc;
        ActorId = actorId;
        Category = category;
        Action = action;
        BusinessEntity = businessEntity;
        EntityIdentifier = entityIdentifier;
        PreviousValue = previousValue;
        NewValue = newValue;
        SourceSystem = sourceSystem;
        ClientApplication = clientApplication;
        IpAddress = ipAddress;
        DeviceInformation = deviceInformation;
        CorrelationId = correlationId;
        Outcome = outcome;
        Metadata = metadata;
    }

    public static Result<AuditRecord> Create(
        DateTimeOffset timestampUtc,
        UserAccountId? actorId,
        AuditCategory category,
        string? action,
        string? businessEntity,
        string? entityIdentifier,
        string? sourceSystem,
        AuditResult result,
        string? previousValue = null,
        string? newValue = null,
        string? clientApplication = null,
        string? ipAddress = null,
        string? deviceInformation = null,
        CorrelationId? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return Result.Failure<AuditRecord>(AuditErrors.ActionRequired);
        }

        if (string.IsNullOrWhiteSpace(businessEntity))
        {
            return Result.Failure<AuditRecord>(AuditErrors.BusinessEntityRequired);
        }

        if (string.IsNullOrWhiteSpace(entityIdentifier))
        {
            return Result.Failure<AuditRecord>(AuditErrors.EntityIdentifierRequired);
        }

        if (string.IsNullOrWhiteSpace(sourceSystem))
        {
            return Result.Failure<AuditRecord>(AuditErrors.SourceSystemRequired);
        }

        return Result.Success(new AuditRecord(
            new AuditRecordId(Guid.NewGuid()),
            timestampUtc,
            actorId,
            category,
            action.Trim(),
            businessEntity.Trim(),
            entityIdentifier.Trim(),
            previousValue,
            newValue,
            sourceSystem.Trim(),
            clientApplication,
            ipAddress,
            deviceInformation,
            correlationId,
            result,
            metadata ?? new Dictionary<string, string>()));
    }
}

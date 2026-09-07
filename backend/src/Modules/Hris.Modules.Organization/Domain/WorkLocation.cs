using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Aggregate Root representing a physical or virtual workplace (examples:
/// "Headquarters", "Branch Office", "Warehouse", "Remote Office"). Source:
/// docs/04-modules/organization/domain/aggregates.md and entities.md, both of which
/// name this as one of the module's own three Aggregate Roots.
///
/// <see cref="OrganizationId"/> is a plain, required, caller-supplied
/// <see cref="Guid"/> to the independent <see cref="Organization"/> Aggregate Root
/// (LOC-003: "Every Work Location must belong to exactly one Organization"),
/// following the identical "reference by identifier, never by instance" rule
/// <see cref="Organization.LegalEntityId"/> already applies to
/// <see cref="LegalEntity"/>. <see cref="LegalEntityId"/> is optional here since
/// LOC-001 through LOC-003 never require one directly on the Location itself --
/// domain/locations.md's own Characteristics list mentions Legal Entity only as one
/// of several optional organizational associations a Location "may be associated
/// with."
/// </summary>
public sealed class WorkLocation : AggregateRoot<WorkLocationId>
{
    public Guid TenantId { get; }

    public LocationCode Code { get; }

    public string Name { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? LegalEntityId { get; private set; }

    public Address Address { get; private set; }

    public WorkLocationTimeZone TimeZone { get; private set; }

    public GeographicLocation? Coordinates { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    // Address, TimeZone, and Coordinates are deliberately not constructor parameters,
    // even though every one of them is required at creation time from this
    // Aggregate's own callers. EF Core's constructor-binding convention refuses to
    // bind any parameter to a navigation property -- Address and Coordinates are
    // each an OwnsOne navigation (WorkLocationConfiguration), and would throw
    // InvalidOperationException ("Cannot bind ... Navigations to related entities,
    // including references to owned types, cannot be bound") at model-build time if
    // included here, found empirically by this Sprint's own model-validation test.
    // TimeZone survives as a constructor parameter because it is a single-column
    // Value Object mapped through HasConversion, a scalar property from EF's own
    // point of view, not a navigation -- the identical distinction
    // UserAccountConfiguration already draws between Username/EmailAddress
    // (HasConversion, constructor-bindable) and an owned type.
    private WorkLocation(
        WorkLocationId id, Guid tenantId, LocationCode code, string name, Guid organizationId, Guid? legalEntityId,
        WorkLocationTimeZone timeZone, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        OrganizationId = organizationId;
        LegalEntityId = legalEntityId;
        Address = null!;
        TimeZone = timeZone;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<WorkLocation> Create(
        WorkLocationId id, Guid tenantId, string? code, string? name, Guid organizationId, Guid? legalEntityId,
        Address address, WorkLocationTimeZone timeZone, GeographicLocation? coordinates, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkLocation>(OrganizationErrors.LocationCodeRequired);
        }

        var codeResult = LocationCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<WorkLocation>(codeResult.Error);
        }

        var workLocation = new WorkLocation(id, tenantId, codeResult.Value, name.Trim(), organizationId, legalEntityId, timeZone, nowUtc)
        {
            Address = address,
            Coordinates = coordinates,
        };
        workLocation.AddDomainEvent(new WorkLocationCreated(Guid.NewGuid(), nowUtc, id, tenantId, codeResult.Value.Value));
        return Result.Success(workLocation);
    }

    public Result Update(
        string? name, Address address, WorkLocationTimeZone timeZone, GeographicLocation? coordinates,
        Guid? legalEntityId, DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(OrganizationErrors.LocationCodeRequired);
        }

        Name = name.Trim();
        Address = address;
        TimeZone = timeZone;
        Coordinates = coordinates;
        LegalEntityId = legalEntityId;
        AddDomainEvent(new WorkLocationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.AlreadyArchived);
        }

        Status = OrganizationalUnitStatus.Archived;
        AddDomainEvent(new WorkLocationArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Restore(DateTimeOffset nowUtc)
    {
        if (Status != OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.NotArchived);
        }

        Status = OrganizationalUnitStatus.Active;
        AddDomainEvent(new WorkLocationRestored(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

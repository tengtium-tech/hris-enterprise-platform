using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Aggregate Root representing a legally registered employer (examples: "ABC
/// Holdings Inc.", "ABC Manufacturing Inc."). Source:
/// docs/04-modules/organization/domain/aggregates.md and entities.md, both of which
/// name this as one of the module's own three Aggregate Roots, independent of
/// <see cref="Organization"/>.
///
/// domain/legal-entities.md's own narrative ("A Legal Entity belongs to an
/// Organization and may contain: Business Units, Departments...") and its own AI
/// Implementation Guidance ("Treat Legal Entities as Organizational Units managed by
/// the Organization Aggregate") directly contradict aggregates.md, entities.md, and
/// infrastructure/persistence.md, all three of which agree with each other that
/// <see cref="LegalEntity"/> is independent and that "Organization references
/// LegalEntityId" (persistence.md, Aggregate References) -- the opposite direction.
/// This type follows the three-way-consistent, more detailed technical
/// specification: no <c>OrganizationId</c> field exists here, and
/// <see cref="Organization.LegalEntityId"/> is the only link between the two,
/// matching aggregates.md's own Legal Entity Purpose statement ("A Legal Entity may
/// own one or more organizations") -- an Organization points at its owning Legal
/// Entity, not the reverse.
///
/// <see cref="Code"/> and <see cref="Name"/> are plain validated strings rather than
/// dedicated Value Objects: domain/value-objects.md's own worked-example list never
/// names a "LegalEntityCode" or "LegalEntityName" type the way it does
/// <see cref="OrganizationCode"/>/<see cref="OrganizationName"/>, so inventing one
/// ahead of a stated need would be exactly the kind of speculative design this
/// project's own documentation-first discipline warns against -- the same choice
/// <c>Connector.Name</c> already makes for itself in the Integration Framework.
/// </summary>
public sealed class LegalEntity : AggregateRoot<LegalEntityId>
{
    public Guid TenantId { get; }

    public string Code { get; }

    public string Name { get; private set; }

    public string? RegisteredBusinessName { get; private set; }

    public BusinessRegistrationNumber BusinessRegistrationNumber { get; }

    public TaxIdentificationNumber? TaxIdentificationNumber { get; private set; }

    public string Country { get; private set; }

    public CurrencyCode? Currency { get; private set; }

    public Address? RegisteredAddress { get; private set; }

    public OrganizationalUnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    // RegisteredAddress is deliberately not a constructor parameter: it is an
    // OwnsOne navigation (LegalEntityConfiguration), and EF Core's constructor-
    // binding convention refuses to bind any parameter to a navigation property --
    // found empirically by this Sprint's own model-validation test, the identical
    // reasoning WorkLocation's own constructor states for Address/Coordinates.
    private LegalEntity(
        LegalEntityId id, Guid tenantId, string code, string name, string? registeredBusinessName,
        BusinessRegistrationNumber businessRegistrationNumber, TaxIdentificationNumber? taxIdentificationNumber,
        string country, CurrencyCode? currency, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        RegisteredBusinessName = registeredBusinessName;
        BusinessRegistrationNumber = businessRegistrationNumber;
        TaxIdentificationNumber = taxIdentificationNumber;
        Country = country;
        Currency = currency;
        Status = OrganizationalUnitStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<LegalEntity> Create(
        LegalEntityId id, Guid tenantId, string? code, string? name, string? registeredBusinessName,
        string? businessRegistrationNumber, TaxIdentificationNumber? taxIdentificationNumber,
        string? country, CurrencyCode? currency, Address? registeredAddress, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<LegalEntity>(OrganizationErrors.LegalEntityCodeRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LegalEntity>(OrganizationErrors.LegalEntityNameRequired);
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<LegalEntity>(OrganizationErrors.AddressCountryRequired);
        }

        var registrationNumberResult = BusinessRegistrationNumber.Create(businessRegistrationNumber);
        if (registrationNumberResult.IsFailure)
        {
            return Result.Failure<LegalEntity>(registrationNumberResult.Error);
        }

        var legalEntity = new LegalEntity(
            id, tenantId, code.Trim(), name.Trim(), registeredBusinessName?.Trim(), registrationNumberResult.Value,
            taxIdentificationNumber, country.Trim(), currency, nowUtc)
        {
            RegisteredAddress = registeredAddress,
        };
        legalEntity.AddDomainEvent(
            new LegalEntityCreated(Guid.NewGuid(), nowUtc, id, tenantId, registrationNumberResult.Value.Value));
        return Result.Success(legalEntity);
    }

    public Result Update(
        string? name, string? registeredBusinessName, TaxIdentificationNumber? taxIdentificationNumber, string? country,
        CurrencyCode? currency, Address? registeredAddress, DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.ArchivedCannotBeModified);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(OrganizationErrors.LegalEntityNameRequired);
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure(OrganizationErrors.AddressCountryRequired);
        }

        Name = name.Trim();
        RegisteredBusinessName = registeredBusinessName?.Trim();
        TaxIdentificationNumber = taxIdentificationNumber;
        Country = country.Trim();
        Currency = currency;
        RegisteredAddress = registeredAddress;
        AddDomainEvent(new LegalEntityUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status == OrganizationalUnitStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.AlreadyArchived);
        }

        Status = OrganizationalUnitStatus.Archived;
        AddDomainEvent(new LegalEntityArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

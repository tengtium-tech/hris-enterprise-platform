using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// Every Domain Event this module's three Aggregate Roots raise. Source:
/// docs/04-modules/organization/domain/domain-events.md, which names each of these
/// by section. All carry <see cref="OrganizationId"/> (or, for
/// <see cref="WorkLocation"/>/<see cref="LegalEntity"/>, their own aggregate id)
/// rather than a synthetic cross-cutting id, since every child entity in the
/// Organization Aggregate's own hierarchy is only ever addressed through the
/// Organization it belongs to. <see cref="TenantId"/> appears only on the *Created
/// events, matching domain-events.md's own explicit OrganizationCreated payload
/// example ("OrganizationId, OrganizationCode, OrganizationName, TenantId,
/// OccurredOn") and this codebase's own established selective-inclusion precedent
/// (compare IntegrationEvents.cs's ConnectorRegistered vs ConnectorUpdated) -- a
/// consumer reacting to a later lifecycle event on an already-known record has no
/// need to be told its tenant a second time.
/// </summary>
public sealed record OrganizationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    Guid TenantId,
    string OrganizationCode,
    string OrganizationName) : IDomainEvent;

public sealed record OrganizationUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId) : IDomainEvent;

public sealed record OrganizationRenamed(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, string NewName) : IDomainEvent;

public sealed record OrganizationArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId) : IDomainEvent;

public sealed record OrganizationRestored(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId) : IDomainEvent;

public sealed record BusinessUnitCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    BusinessUnitId BusinessUnitId,
    string BusinessUnitName) : IDomainEvent;

public sealed record BusinessUnitRenamed(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, BusinessUnitId BusinessUnitId) : IDomainEvent;

public sealed record BusinessUnitArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, BusinessUnitId BusinessUnitId) : IDomainEvent;

public sealed record DivisionCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    DivisionId DivisionId,
    BusinessUnitId BusinessUnitId,
    string DivisionName) : IDomainEvent;

public sealed record DivisionRenamed(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, DivisionId DivisionId) : IDomainEvent;

public sealed record DivisionMoved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    DivisionId DivisionId,
    BusinessUnitId NewBusinessUnitId) : IDomainEvent;

public sealed record DivisionArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, DivisionId DivisionId) : IDomainEvent;

public sealed record DepartmentCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    DepartmentId DepartmentId,
    DivisionId DivisionId,
    string DepartmentName,
    string DepartmentCode) : IDomainEvent;

public sealed record DepartmentRenamed(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, DepartmentId DepartmentId) : IDomainEvent;

public sealed record DepartmentMoved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    DepartmentId DepartmentId,
    DivisionId NewDivisionId) : IDomainEvent;

public sealed record DepartmentMerged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    DepartmentId SurvivingDepartmentId,
    IReadOnlyList<DepartmentId> MergedDepartmentIds) : IDomainEvent;

public sealed record DepartmentSplit(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    DepartmentId SourceDepartmentId,
    IReadOnlyList<DepartmentId> NewDepartmentIds) : IDomainEvent;

public sealed record DepartmentArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, DepartmentId DepartmentId) : IDomainEvent;

public sealed record DepartmentRestored(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, DepartmentId DepartmentId) : IDomainEvent;

public sealed record SectionCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    SectionId SectionId,
    DepartmentId DepartmentId,
    string SectionName) : IDomainEvent;

public sealed record SectionRenamed(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, SectionId SectionId) : IDomainEvent;

public sealed record SectionArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, SectionId SectionId) : IDomainEvent;

public sealed record TeamCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    TeamId TeamId,
    SectionId SectionId,
    string TeamName) : IDomainEvent;

public sealed record TeamRenamed(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, TeamId TeamId) : IDomainEvent;

public sealed record TeamArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, TeamId TeamId) : IDomainEvent;

public sealed record CostCenterCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    OrganizationId OrganizationId,
    CostCenterId CostCenterId,
    string CostCenterCode) : IDomainEvent;

public sealed record CostCenterUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, CostCenterId CostCenterId) : IDomainEvent;

public sealed record CostCenterArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, OrganizationId OrganizationId, CostCenterId CostCenterId) : IDomainEvent;

public sealed record WorkLocationCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkLocationId WorkLocationId,
    Guid TenantId,
    string LocationCode) : IDomainEvent;

public sealed record WorkLocationUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, WorkLocationId WorkLocationId) : IDomainEvent;

public sealed record WorkLocationArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, WorkLocationId WorkLocationId) : IDomainEvent;

public sealed record WorkLocationRestored(
    Guid EventId, DateTimeOffset OccurredOnUtc, WorkLocationId WorkLocationId) : IDomainEvent;

public sealed record LegalEntityCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    LegalEntityId LegalEntityId,
    Guid TenantId,
    string BusinessRegistrationNumber) : IDomainEvent;

public sealed record LegalEntityUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, LegalEntityId LegalEntityId) : IDomainEvent;

public sealed record LegalEntityArchived(
    Guid EventId, DateTimeOffset OccurredOnUtc, LegalEntityId LegalEntityId) : IDomainEvent;

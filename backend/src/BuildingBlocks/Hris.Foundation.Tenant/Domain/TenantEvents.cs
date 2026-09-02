using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// All nine of tenant-framework.md's own Domain Events section, field-for-field.
/// <see cref="TenantActivated.ActivatedBy"/> is a <see cref="UserAccountId"/> -- the
/// invited Tenant Administrator's own account, per that section's own "Raised when
/// ActivateTenantCommand succeeds" -- while every other actor field here is a
/// <see cref="PlatformOperatorId"/>, per ADR-0009's "different account universe":
/// <c>ActivateTenantCommand</c> is this document's own stated exception, "invoked by
/// the invited Tenant Administrator, not a Platform Operator."
/// </summary>
public sealed record TenantCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    TenantCode TenantCode,
    string Organization,
    SubscriptionPlan SubscriptionPlan,
    PlatformOperatorId? RegisteredBy) : IDomainEvent;

public sealed record TenantProvisioned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    TenantConfigurationId TenantConfigurationId) : IDomainEvent;

public sealed record TenantActivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    UserAccountId ActivatedBy) : IDomainEvent;

public sealed record TenantSuspended(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    PlatformOperatorId SuspendedBy,
    string Reason) : IDomainEvent;

public sealed record TenantReactivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    PlatformOperatorId ReactivatedBy) : IDomainEvent;

public sealed record TenantArchived(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    PlatformOperatorId ArchivedBy,
    string Reason) : IDomainEvent;

public sealed record TenantDeleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    PlatformOperatorId DeletedBy,
    string Reason,
    string ComplianceBasis) : IDomainEvent;

/// <summary>
/// <see cref="PacksActivated"/> is always empty in this Sprint's own build -- per
/// tenant-framework.md's own Platform-Operator-Facing Commands table, a plan change
/// automatically activates any pack the target edition newly includes by default, but
/// that activation is `administration`'s own <c>TenantConfiguration</c> Process Pack
/// Activation mechanism (tenant-configuration.md), which does not exist in code yet --
/// see <see cref="Tenant.ChangeSubscriptionPlan"/>'s own remarks. The field is kept on
/// this event, rather than dropped, because tenant-framework.md's own field list
/// already names it; a future pass that wires up Process Pack Activation populates it
/// here, not by changing this event's shape.
/// </summary>
public sealed record TenantLicenseUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    SubscriptionPlan PreviousSubscriptionPlan,
    SubscriptionPlan NewSubscriptionPlan,
    PlatformOperatorId ChangedBy,
    IReadOnlyCollection<string> PacksActivated) : IDomainEvent;

public sealed record TenantUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    TenantId TenantId,
    string PreviousOrganization,
    string NewOrganization,
    PlatformOperatorId UpdatedBy) : IDomainEvent;

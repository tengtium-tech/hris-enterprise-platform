using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Every Domain Event the <see cref="Employee"/> Aggregate Root raises. Source:
/// docs/04-modules/employee/domain/domain-events.md and employee-lifecycle.md's own
/// "Lifecycle Events" section, which together name each of these. Cross-aggregate
/// references (none exist here -- Employee has no compile-time reference to
/// Employment) would use plain <see cref="Guid"/>, per this platform's own standing
/// rule; every identifier below instead belongs to Employee's own aggregate
/// boundary (<see cref="EmployeeId"/> itself, or a child entity's own strongly
/// typed ID), so each is carried in its own strongly typed form, matching
/// Employment's own <c>CompensationRecordId</c> precedent for same-aggregate child
/// identifiers.
///
/// <see cref="EmployeeOffboardingStarted"/> and <see cref="EmployeeRetired"/> have
/// no name given anywhere in domain-events.md, which never explicitly names an
/// event for the Active-to-Offboarding or Separated-to-Retired transitions despite
/// BR-EMP-018 requiring both stages be reachable; inferred here following this
/// document's own stated convention (<c>Employee&lt;PastTenseVerb&gt;</c>), the
/// identical gap-filling precedent Employment's own domain-events.md documented for
/// <c>EmploymentSeconded</c>.
/// </summary>
public sealed record EmployeeCreated(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, Guid TenantId, string EmployeeNumber,
    string FirstName, string LastName) : IDomainEvent;

public sealed record EmployeeOnboardingStarted(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeActivated(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeOffboardingStarted(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeSeparated(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeRetired(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeDeceased(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeePersonalInformationUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeContactInformationUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeGovernmentInformationUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmployeeBankingInformationUpdated(Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId) : IDomainEvent;

public sealed record EmergencyContactAdded(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, EmergencyContactId EmergencyContactId) : IDomainEvent;

public sealed record EmergencyContactUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, EmergencyContactId EmergencyContactId) : IDomainEvent;

public sealed record EmergencyContactRemoved(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, EmergencyContactId EmergencyContactId) : IDomainEvent;

public sealed record FamilyMemberAdded(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, FamilyMemberId FamilyMemberId) : IDomainEvent;

public sealed record FamilyMemberUpdated(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, FamilyMemberId FamilyMemberId) : IDomainEvent;

public sealed record FamilyMemberRemoved(
    Guid EventId, DateTimeOffset OccurredOnUtc, EmployeeId EmployeeId, FamilyMemberId FamilyMemberId) : IDomainEvent;

using Hris.Foundation.Identity.Domain;

namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// The filter dimensions audit-framework.md's Audit Search section names: "Date
/// Range, User, Business Entity, Action, Company, Department, Location, Correlation
/// Identifier, Result, Module." A plain filter bag, not a Value Object -- nothing
/// compares two searches for business equality, so the validation/equality machinery
/// <c>ValueObject</c> exists for would add nothing here.
///
/// <see cref="CompanyId"/>, <see cref="DepartmentId"/>, and <see cref="Location"/>
/// are this document's own named filters, kept as raw, optional identifiers since
/// Organization (which would own <c>CompanyId</c>/<c>DepartmentId</c> as strongly
/// typed values) does not exist until Phase 2 (`CTR-ARC-002`).
/// </summary>
public sealed record AuditSearchCriteria(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    UserAccountId? ActorId = null,
    string? BusinessEntity = null,
    string? Action = null,
    Guid? CompanyId = null,
    Guid? DepartmentId = null,
    string? Location = null,
    Guid? CorrelationId = null,
    AuditResult? Outcome = null,
    string? SourceSystem = null);

using Hris.SharedKernel;

namespace Hris.Foundation.Audit.Domain;

/// <summary>Identity of an <see cref="AuditRecord"/>. Source: docs/03-foundation/audit-framework.md.</summary>
public readonly record struct AuditRecordId(Guid Value) : IStronglyTypedId;

namespace Hris.Modules.Leave.Application.Dtos;

/// <summary>Read shape for <c>LeaveBalance</c> (dto-design.md). Carries no ledger inline — see <c>LeaveLedgerEntryDto</c>.</summary>
public sealed record LeaveBalanceDto(
    Guid Id, Guid TenantId, Guid EmployeeId, Guid LeaveTypeId, decimal CurrentBalance, DateTimeOffset? LastRecalculatedAt);

/// <summary>Read shape for one <c>LeaveLedgerEntry</c>, returned only as part of a balance's full ledger.</summary>
public sealed record LeaveLedgerEntryDto(
    Guid Id, string EntryType, decimal Amount, DateOnly EffectiveDate, Guid SourceReference, Guid? Actor, DateTimeOffset RecordedAt);

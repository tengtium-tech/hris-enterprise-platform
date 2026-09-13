namespace Hris.Modules.Leave.Application.Dtos;

/// <summary>Read shape for <c>LeaveType</c> (dto-design.md).</summary>
public sealed record LeaveTypeDto(
    Guid Id,
    Guid? TenantId,
    string Code,
    string Name,
    string Category,
    string Scope,
    string? StatutoryBasis,
    decimal? StatutoryMinimum,
    string Status);

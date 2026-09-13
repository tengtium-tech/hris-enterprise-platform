namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>
/// Read shape for a biometric enrollment's status. Defined with <em>no</em> property capable
/// of holding template content — not merely an omitted field — so no mapping code can
/// accidentally populate it and no field-security rule can fail open (dto-design.md,
/// ../domain/attendance-devices.md). Only the <c>BiometricTemplateReference</c> pointer
/// exists in the domain, and it is intentionally not surfaced here either.
/// </summary>
public sealed record BiometricEnrollmentStatusDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    string Method,
    string Status,
    string? Vendor,
    IReadOnlyList<Guid> SynchronizedDeviceIds);

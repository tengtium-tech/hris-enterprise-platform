namespace Hris.Modules.Employee.Application.Dtos;

public sealed record EmployeeHistoryDto(
    Guid Id,
    Guid EmployeeId,
    string Category,
    string? PreviousValue,
    string? NewValue,
    DateOnly EffectiveDate,
    string? BusinessReason,
    Guid? ChangedBy,
    DateTimeOffset CreatedAtUtc);

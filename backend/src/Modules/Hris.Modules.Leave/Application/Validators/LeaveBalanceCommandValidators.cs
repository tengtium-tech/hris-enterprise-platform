using FluentValidation;
using Hris.Modules.Leave.Application.Commands;

namespace Hris.Modules.Leave.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>LeaveBalance</c> commands. Ledger immutability
/// and reproducibility (LV-020, LV-021) belong to the aggregate; here we confirm the
/// request is well-formed (application/validations.md).
/// </summary>
public sealed class RecordLeaveAccrualCommandValidator : AbstractValidator<RecordLeaveAccrualCommand>
{
    public RecordLeaveAccrualCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.LeaveTypeId).NotEmpty();
        RuleFor(c => c.SourceReference).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}

public sealed class RecordCarryoverCommandValidator : AbstractValidator<RecordCarryoverCommand>
{
    public RecordCarryoverCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.SourceReference).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}

public sealed class RecordForfeitureCommandValidator : AbstractValidator<RecordForfeitureCommand>
{
    public RecordForfeitureCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.SourceReference).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}

public sealed class RecalculateLeaveBalanceCommandValidator : AbstractValidator<RecalculateLeaveBalanceCommand>
{
    public RecalculateLeaveBalanceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveBalanceId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

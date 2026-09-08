using Hris.Application.Abstractions;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Commands;

public sealed record RecordEmploymentCompensationCommand(
    Guid EmploymentId,
    Guid TenantId,
    decimal Amount,
    string? CurrencyCode,
    CompensationBasis Basis,
    DateOnly EffectiveStartDate,
    CompensationChangeSource ChangeSource,
    string? ApprovalReference) : ICommand<Result>;

internal sealed class RecordEmploymentCompensationCommandHandler
    : IRequestHandler<RecordEmploymentCompensationCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordEmploymentCompensationCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordEmploymentCompensationCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.RecordCompensation(
                request.Amount, request.CurrencyCode, request.Basis, request.EffectiveStartDate, request.ChangeSource,
                request.ApprovalReference, _timeProvider.GetUtcNow());
    }
}

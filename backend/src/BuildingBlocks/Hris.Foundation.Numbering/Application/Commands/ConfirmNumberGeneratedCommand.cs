using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// Confirms a reserved number as finalized -- see <see cref="IssuedNumber.MarkGenerated"/>.
/// </summary>
public sealed record ConfirmNumberGeneratedCommand(Guid IssuedNumberId) : ICommand<Result>;

internal sealed class ConfirmNumberGeneratedCommandHandler : IRequestHandler<ConfirmNumberGeneratedCommand, Result>
{
    private readonly IIssuedNumberRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmNumberGeneratedCommandHandler(IIssuedNumberRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ConfirmNumberGeneratedCommand request, CancellationToken cancellationToken)
    {
        var issuedNumber = await _repository.GetByIdAsync(new IssuedNumberId(request.IssuedNumberId), cancellationToken).ConfigureAwait(false);
        if (issuedNumber is null)
        {
            return Result.Failure(NumberingErrors.IssuedNumberNotFound);
        }

        return issuedNumber.MarkGenerated(_timeProvider.GetUtcNow());
    }
}

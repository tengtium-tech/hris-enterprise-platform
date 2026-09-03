using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// Attaches an issued number to the business record that consumes it -- see
/// <see cref="IssuedNumber.Assign"/>.
/// </summary>
public sealed record AssignNumberCommand(Guid IssuedNumberId, string AssignedToType, string AssignedToReferenceId) : ICommand<Result>;

internal sealed class AssignNumberCommandHandler : IRequestHandler<AssignNumberCommand, Result>
{
    private readonly IIssuedNumberRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignNumberCommandHandler(IIssuedNumberRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AssignNumberCommand request, CancellationToken cancellationToken)
    {
        var issuedNumber = await _repository.GetByIdAsync(new IssuedNumberId(request.IssuedNumberId), cancellationToken).ConfigureAwait(false);
        if (issuedNumber is null)
        {
            return Result.Failure(NumberingErrors.IssuedNumberNotFound);
        }

        return issuedNumber.Assign(request.AssignedToType, request.AssignedToReferenceId, _timeProvider.GetUtcNow());
    }
}

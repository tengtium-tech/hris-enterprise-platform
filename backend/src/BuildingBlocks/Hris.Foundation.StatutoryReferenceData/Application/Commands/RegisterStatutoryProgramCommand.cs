using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Commands;

/// <summary>
/// Registers a new statutory program within a country's own scope. Carries raw
/// primitives, not Domain Value Objects, across the MediatR boundary -- this handler is
/// the one place a malformed code or country becomes a
/// <see cref="StatutoryReferenceDataErrors"/> failure. A Platform-administration-only
/// operation -- statutory-reference-data.md's own Security Considerations: "Modification
/// is restricted to platform administration and is never exposed to tenants" -- the
/// actual RBAC check itself is deferred, the identical reasoning every other Sprint 4
/// framework's own remarks state for Authorization Framework's concrete wiring.
/// </summary>
public sealed record RegisterStatutoryProgramCommand(
    string Code,
    string Country,
    string DisplayName) : ICommand<Result<Guid>>;

internal sealed class RegisterStatutoryProgramCommandHandler : IRequestHandler<RegisterStatutoryProgramCommand, Result<Guid>>
{
    private readonly IStatutoryProgramRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterStatutoryProgramCommandHandler(IStatutoryProgramRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterStatutoryProgramCommand request, CancellationToken cancellationToken)
    {
        var codeResult = StatutoryProgramCode.Create(request.Code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<Guid>(codeResult.Error);
        }

        var countryResult = StatutoryCountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure<Guid>(countryResult.Error);
        }

        if (await _repository.ExistsByCodeAndCountryAsync(codeResult.Value, countryResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(StatutoryReferenceDataErrors.DuplicateProgramCode);
        }

        var registerResult = StatutoryProgram.Register(
            codeResult.Value, countryResult.Value, request.DisplayName, _timeProvider.GetUtcNow());
        if (registerResult.IsFailure)
        {
            return Result.Failure<Guid>(registerResult.Error);
        }

        await _repository.AddAsync(registerResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registerResult.Value.Id.Value);
    }
}

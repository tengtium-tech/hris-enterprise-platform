using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// One of identity-framework.md's five Client-Facing Commands: "Factor type (bounded
/// by the Multi-Factor Authentication section's own supported-factor list above),
/// actor." <see cref="FactorType"/> already carries that bound structurally --
/// <see cref="MfaFactorType"/> is a closed enum of exactly the six factors that
/// section names, so an out-of-range value is a compile-time impossibility for any
/// caller within this solution, not a runtime check this handler needs to perform.
///
/// Delegates producing the opaque <c>SecretReference</c> to
/// <see cref="IMfaSecretProvisioner"/> -- see that interface's own remarks for why
/// generating the actual factor material is Infrastructure, never this handler's job.
/// </summary>
public sealed record EnrollMfaFactorCommand(
    Guid ActorUserAccountId,
    Guid TenantId,
    MfaFactorType FactorType) : ICommand<Result<Guid>>;

internal sealed class EnrollMfaFactorCommandHandler : IRequestHandler<EnrollMfaFactorCommand, Result<Guid>>
{
    private readonly IUserAccountRepository _repository;
    private readonly IMfaSecretProvisioner _secretProvisioner;
    private readonly TimeProvider _timeProvider;

    public EnrollMfaFactorCommandHandler(
        IUserAccountRepository repository,
        IMfaSecretProvisioner secretProvisioner,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _secretProvisioner = Guard.AgainstNull(secretProvisioner, nameof(secretProvisioner));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(EnrollMfaFactorCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.ActorUserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Failure<Guid>(IdentityErrors.AccountNotFound);
        }

        var secretReference = await _secretProvisioner
            .ProvisionAsync(request.FactorType, cancellationToken)
            .ConfigureAwait(false);

        var factorResult = account.EnrollMfaFactor(request.FactorType, secretReference, _timeProvider.GetUtcNow());

        return factorResult.IsFailure
            ? Result.Failure<Guid>(factorResult.Error)
            : Result.Success(factorResult.Value.Id.Value);
    }
}

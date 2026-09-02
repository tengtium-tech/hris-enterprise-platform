using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Commands;

/// <summary>
/// Registers a new Extension Point in Draft status, per extension-framework.md's own
/// Core Concepts section. Carries raw primitives, not Domain Value Objects, across the
/// MediatR boundary -- <see cref="RegisterExtensionPointCommandHandler"/> is the one
/// place a malformed key becomes an <see cref="ExtensionErrors"/> failure.
///
/// Not authorization-gated, matching Localization Framework's own established
/// precedent rather than Rules Engine's: an Extension Point is platform-wide contract
/// registry data (used by "All Business Modules" across every tenant, per this
/// document's own Downstream Consumers), not tenant-scoped business data, and
/// <c>OrganizationalScopeLevel</c> has no Global level to check a platform-wide
/// registration against without inventing a placeholder tenant id -- the identical
/// reasoning <c>CreateCountryConfigurationCommandHandler</c>'s own remarks state.
/// Registering an extension point is also, unlike publishing or modifying a tenant's
/// own business rule, an internal-platform-developer act (declaring "this is a valid
/// customization point" as a module is built), not something this document's own
/// Security section's extension-<em>execution</em> controls (RBAC, tenant isolation)
/// govern -- those apply once a real execution engine exists to invoke a Hook, which
/// this Sprint's own build does not yet have.
/// </summary>
public sealed record RegisterExtensionPointCommand(
    string Key,
    string Name,
    string? Description,
    ExtensionPointType ExtensionPointType,
    string OwningModule,
    IReadOnlyCollection<HookType> SupportedHookTypes) : ICommand<Result<Guid>>;

internal sealed class RegisterExtensionPointCommandHandler : IRequestHandler<RegisterExtensionPointCommand, Result<Guid>>
{
    private readonly IExtensionPointRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterExtensionPointCommandHandler(IExtensionPointRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterExtensionPointCommand request, CancellationToken cancellationToken)
    {
        var keyResult = ExtensionPointKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<Guid>(keyResult.Error);
        }

        if (await _repository.ExistsByKeyAsync(keyResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(ExtensionErrors.ExtensionPointKeyAlreadyRegistered);
        }

        var registrationResult = ExtensionPoint.Register(
            keyResult.Value,
            request.Name,
            request.Description,
            request.ExtensionPointType,
            request.OwningModule,
            request.SupportedHookTypes,
            _timeProvider.GetUtcNow());

        if (registrationResult.IsFailure)
        {
            return Result.Failure<Guid>(registrationResult.Error);
        }

        await _repository.AddAsync(registrationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registrationResult.Value.Id.Value);
    }
}

using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Commands;

/// <summary>
/// Registers a new Hook against an Extension Point. This is the one command in this
/// framework that genuinely needs cross-aggregate validation before construction --
/// "Only published extension points should be used" (extension-framework.md, Core
/// Concepts) and the requested <see cref="HookType"/> must be one the target point's
/// own <see cref="ExtensionPoint.SupportedHookTypes"/> declares -- and per
/// <see cref="Hook.Register"/>'s own remarks, a Value Object/Aggregate factory
/// validates its own shape only, never another aggregate's current state. Loading the
/// <see cref="ExtensionPoint"/> and checking both here, in the handler, before calling
/// <see cref="Hook.Register"/>, is that split applied for real rather than only
/// documented as a principle.
/// </summary>
public sealed record RegisterHookCommand(
    Guid ExtensionPointId,
    HookType HookType,
    string HandlerReference,
    string OwningModule) : ICommand<Result<Guid>>;

internal sealed class RegisterHookCommandHandler : IRequestHandler<RegisterHookCommand, Result<Guid>>
{
    private readonly IExtensionPointRepository _extensionPointRepository;
    private readonly IHookRepository _hookRepository;
    private readonly TimeProvider _timeProvider;

    public RegisterHookCommandHandler(
        IExtensionPointRepository extensionPointRepository,
        IHookRepository hookRepository,
        TimeProvider timeProvider)
    {
        _extensionPointRepository = Guard.AgainstNull(extensionPointRepository, nameof(extensionPointRepository));
        _hookRepository = Guard.AgainstNull(hookRepository, nameof(hookRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterHookCommand request, CancellationToken cancellationToken)
    {
        var extensionPointId = new ExtensionPointId(request.ExtensionPointId);
        var extensionPoint = await _extensionPointRepository.GetByIdAsync(extensionPointId, cancellationToken).ConfigureAwait(false);
        if (extensionPoint is null)
        {
            return Result.Failure<Guid>(ExtensionErrors.ExtensionPointNotFound);
        }

        if (extensionPoint.Status != ExtensionPointStatus.Published)
        {
            return Result.Failure<Guid>(ExtensionErrors.ExtensionPointNotPublished);
        }

        if (!extensionPoint.SupportedHookTypes.Contains(request.HookType))
        {
            return Result.Failure<Guid>(ExtensionErrors.HookTypeNotSupportedByExtensionPoint);
        }

        var registrationResult = Hook.Register(
            extensionPointId,
            request.HookType,
            request.HandlerReference,
            request.OwningModule,
            _timeProvider.GetUtcNow());

        if (registrationResult.IsFailure)
        {
            return Result.Failure<Guid>(registrationResult.Error);
        }

        await _hookRepository.AddAsync(registrationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registrationResult.Value.Id.Value);
    }
}

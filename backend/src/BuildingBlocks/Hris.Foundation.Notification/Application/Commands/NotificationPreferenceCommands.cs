using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Commands;

public sealed record RegisterNotificationPreferenceCommand(
    Guid TenantId,
    Guid UserId,
    string? PreferredLanguage,
    IReadOnlyList<NotificationChannel> PreferredChannels,
    TimeSpan? QuietHoursStart,
    TimeSpan? QuietHoursEnd,
    bool DigestMode,
    bool OptedOut) : ICommand<Result<Guid>>;

public sealed record UpdateNotificationPreferenceCommand(
    Guid NotificationPreferenceId,
    string? PreferredLanguage,
    IReadOnlyList<NotificationChannel> PreferredChannels,
    TimeSpan? QuietHoursStart,
    TimeSpan? QuietHoursEnd,
    bool DigestMode,
    bool OptedOut) : ICommand<Result>;

internal sealed class RegisterNotificationPreferenceCommandHandler
    : IRequestHandler<RegisterNotificationPreferenceCommand, Result<Guid>>
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterNotificationPreferenceCommandHandler(INotificationPreferenceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByUserAsync(request.TenantId, request.UserId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(NotificationErrors.PreferenceAlreadyExists);
        }

        var registerResult = NotificationPreference.Register(
            request.TenantId, request.UserId, request.PreferredLanguage, request.PreferredChannels,
            request.QuietHoursStart, request.QuietHoursEnd, request.DigestMode, request.OptedOut, _timeProvider.GetUtcNow());
        if (registerResult.IsFailure)
        {
            return Result.Failure<Guid>(registerResult.Error);
        }

        await _repository.AddAsync(registerResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registerResult.Value.Id.Value);
    }
}

internal sealed class UpdateNotificationPreferenceCommandHandler : IRequestHandler<UpdateNotificationPreferenceCommand, Result>
{
    private readonly INotificationPreferenceRepository _repository;

    public UpdateNotificationPreferenceCommandHandler(INotificationPreferenceRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var preference = await _repository.GetByIdAsync(
            new NotificationPreferenceId(request.NotificationPreferenceId), cancellationToken).ConfigureAwait(false);
        if (preference is null)
        {
            return Result.Failure(NotificationErrors.PreferenceNotFound);
        }

        return preference.Update(
            request.PreferredLanguage, request.PreferredChannels, request.QuietHoursStart, request.QuietHoursEnd,
            request.DigestMode, request.OptedOut);
    }
}

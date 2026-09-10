using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>
/// Authors a holiday calendar in Draft.
///
/// <see cref="ActingAsPlatform"/> is what separates a platform administrator
/// publishing the statutory national layer from a tenant authoring its own company
/// calendar. There is deliberately no command letting a tenant edit the Country
/// layer at all (TK-041) — the Philippine regular and special holiday calendar is
/// published by government proclamation and identical for every employer, so letting
/// each tenant transcribe it independently would reproduce exactly the correctness
/// and liability problems statutory-reference-data.md argues against for
/// contribution tables.
/// </summary>
public sealed record DefineHolidayCalendarCommand(
    Guid TenantId,
    string? Name,
    HolidayCalendarLevel Level,
    string? ScopeTargetId,
    string? CountryCode,
    Guid? ParentCalendarId,
    DateOnly EffectiveFrom,
    bool ActingAsPlatform,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class DefineHolidayCalendarCommandHandler : IRequestHandler<DefineHolidayCalendarCommand, Result<Guid>>
{
    private readonly IHolidayCalendarRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineHolidayCalendarCommandHandler(IHolidayCalendarRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineHolidayCalendarCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        if (request.Level == HolidayCalendarLevel.Country && !request.ActingAsPlatform)
        {
            return Result.Failure<Guid>(TimekeepingErrors.CountryLayerIsReadOnlyToTenant);
        }

        // A country calendar is platform data with no owning tenant; everything above
        // it belongs to the tenant that authored it.
        var owningTenant = request.Level == HolidayCalendarLevel.Country ? (Guid?)null : request.TenantId;

        var result = HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), owningTenant, request.Name, request.Level, request.ScopeTargetId,
            request.CountryCode,
            request.ParentCalendarId is null ? null : new HolidayCalendarId(request.ParentCalendarId.Value),
            request.EffectiveFrom, request.ActingUser, _timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

public sealed record AddHolidayCommand(
    Guid TenantId,
    Guid HolidayCalendarId,
    DateOnly Date,
    string? Name,
    HolidayType Type,
    HolidayWorkRule WorkRule,
    bool ActingAsPlatform,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class AddHolidayCommandHandler : IRequestHandler<AddHolidayCommand, Result<Guid>>
{
    private readonly IHolidayCalendarRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AddHolidayCommandHandler(IHolidayCalendarRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AddHolidayCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var calendarResult = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.HolidayCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (calendarResult.IsFailure)
        {
            return Result.Failure<Guid>(calendarResult.Error);
        }

        var result = calendarResult.Value.AddHoliday(
            new HolidayId(Guid.NewGuid()), request.Date, request.Name, request.Type, request.WorkRule,
            request.ActingAsPlatform, request.ActingUser, _timeProvider.GetUtcNow());

        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record RemoveHolidayCommand(
    Guid TenantId, Guid HolidayCalendarId, Guid HolidayId, bool ActingAsPlatform, Guid ActingUser) : ICommand<Result>;

internal sealed class RemoveHolidayCommandHandler : IRequestHandler<RemoveHolidayCommand, Result>
{
    private readonly IHolidayCalendarRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveHolidayCommandHandler(IHolidayCalendarRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveHolidayCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var calendarResult = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.HolidayCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return calendarResult.IsFailure
            ? Result.Failure(calendarResult.Error)
            : calendarResult.Value.RemoveHoliday(
                new HolidayId(request.HolidayId), request.ActingAsPlatform, request.ActingUser,
                _timeProvider.GetUtcNow());
    }
}

public sealed record PublishHolidayCalendarCommand(Guid TenantId, Guid HolidayCalendarId, Guid ActingUser)
    : ICommand<Result>;

internal sealed class PublishHolidayCalendarCommandHandler : IRequestHandler<PublishHolidayCalendarCommand, Result>
{
    private readonly IHolidayCalendarRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishHolidayCalendarCommandHandler(IHolidayCalendarRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishHolidayCalendarCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var calendarResult = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.HolidayCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return calendarResult.IsFailure
            ? Result.Failure(calendarResult.Error)
            : calendarResult.Value.Publish(request.ActingUser, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Produces the next calendar version as a Draft carrying a copy of the current
/// entries (TK-044). The copy is what makes revising practical without re-entering a
/// year of holidays.
/// </summary>
public sealed record ReviseHolidayCalendarCommand(
    Guid TenantId, Guid HolidayCalendarId, DateOnly NewEffectiveFrom, Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class ReviseHolidayCalendarCommandHandler : IRequestHandler<ReviseHolidayCalendarCommand, Result<Guid>>
{
    private readonly IHolidayCalendarRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviseHolidayCalendarCommandHandler(IHolidayCalendarRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(ReviseHolidayCalendarCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var calendarResult = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.HolidayCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (calendarResult.IsFailure)
        {
            return Result.Failure<Guid>(calendarResult.Error);
        }

        var nextResult = calendarResult.Value.Supersede(
            new HolidayCalendarId(Guid.NewGuid()), request.NewEffectiveFrom, request.ActingUser,
            _timeProvider.GetUtcNow());

        if (nextResult.IsFailure)
        {
            return Result.Failure<Guid>(nextResult.Error);
        }

        await _repository.AddAsync(nextResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(nextResult.Value.Id.Value);
    }
}

/// <summary>
/// Re-points a calendar's parent layer. Its own event exists because this changes
/// what the resolved holiday set for the child scope is without touching any of the
/// child's own entries.
/// </summary>
public sealed record ChangeHolidayCalendarLayerCommand(
    Guid TenantId, Guid HolidayCalendarId, Guid? NewParentCalendarId, Guid ActingUser) : ICommand<Result>;

internal sealed class ChangeHolidayCalendarLayerCommandHandler
    : IRequestHandler<ChangeHolidayCalendarLayerCommand, Result>
{
    private readonly IHolidayCalendarRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeHolidayCalendarLayerCommandHandler(IHolidayCalendarRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeHolidayCalendarLayerCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var calendarResult = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.HolidayCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return calendarResult.IsFailure
            ? Result.Failure(calendarResult.Error)
            : calendarResult.Value.ChangeParentCalendar(
                request.NewParentCalendarId is null ? null : new HolidayCalendarId(request.NewParentCalendarId.Value),
                request.ActingUser, _timeProvider.GetUtcNow());
    }
}

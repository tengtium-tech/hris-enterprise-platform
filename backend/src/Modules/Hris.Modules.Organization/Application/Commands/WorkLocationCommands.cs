using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Work Location Aggregate's own lifecycle commands, including LOC-001's own
/// tenant-wide code uniqueness check. Address/TimeZone/coordinates arrive as raw
/// primitives across the MediatR boundary, the same "primitives across the wire,
/// Value Objects inside the Domain" choice every other command in this codebase
/// already makes; this handler assembles the <see cref="Address"/>,
/// <see cref="WorkLocationTimeZone"/>, and optional <see cref="GeographicLocation"/>
/// Value Objects before calling <see cref="WorkLocation.Create"/>/
/// <see cref="WorkLocation.Update"/>, since those Value Objects are not
/// themselves dereferenced-and-guarded inside the Aggregate the way
/// <see cref="LocationCode"/> is.
/// </summary>
public sealed record CreateWorkLocationCommand(
    Guid TenantId,
    string? Code,
    string? Name,
    Guid OrganizationId,
    Guid? LegalEntityId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? ProvinceOrState,
    string? PostalCode,
    string? Country,
    string? TimeZone,
    double? Latitude,
    double? Longitude) : ICommand<Result<Guid>>;

internal sealed class CreateWorkLocationCommandHandler : IRequestHandler<CreateWorkLocationCommand, Result<Guid>>
{
    private readonly IWorkLocationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateWorkLocationCommandHandler(IWorkLocationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateWorkLocationCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsWithCodeAsync(request.TenantId, request.Code, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateLocationCode);
        }

        var addressResult = Address.Create(
            request.AddressLine1, request.AddressLine2, request.City, request.ProvinceOrState, request.PostalCode, request.Country);
        if (addressResult.IsFailure)
        {
            return Result.Failure<Guid>(addressResult.Error);
        }

        var timeZoneResult = WorkLocationTimeZone.Create(request.TimeZone);
        if (timeZoneResult.IsFailure)
        {
            return Result.Failure<Guid>(timeZoneResult.Error);
        }

        GeographicLocation? coordinates = null;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var coordinatesResult = GeographicLocation.Create(request.Latitude.Value, request.Longitude.Value);
            if (coordinatesResult.IsFailure)
            {
                return Result.Failure<Guid>(coordinatesResult.Error);
            }

            coordinates = coordinatesResult.Value;
        }

        var workLocationResult = WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, request.OrganizationId,
            request.LegalEntityId, addressResult.Value, timeZoneResult.Value, coordinates, _timeProvider.GetUtcNow());
        if (workLocationResult.IsFailure)
        {
            return Result.Failure<Guid>(workLocationResult.Error);
        }

        await _repository.AddAsync(workLocationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(workLocationResult.Value.Id.Value);
    }
}

public sealed record UpdateWorkLocationCommand(
    Guid WorkLocationId,
    Guid TenantId,
    string? Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? ProvinceOrState,
    string? PostalCode,
    string? Country,
    string? TimeZone,
    double? Latitude,
    double? Longitude,
    Guid? LegalEntityId) : ICommand<Result>;

internal sealed class UpdateWorkLocationCommandHandler : IRequestHandler<UpdateWorkLocationCommand, Result>
{
    private readonly IWorkLocationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateWorkLocationCommandHandler(IWorkLocationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateWorkLocationCommand request, CancellationToken cancellationToken)
    {
        var addressResult = Address.Create(
            request.AddressLine1, request.AddressLine2, request.City, request.ProvinceOrState, request.PostalCode, request.Country);
        if (addressResult.IsFailure)
        {
            return Result.Failure(addressResult.Error);
        }

        var timeZoneResult = WorkLocationTimeZone.Create(request.TimeZone);
        if (timeZoneResult.IsFailure)
        {
            return Result.Failure(timeZoneResult.Error);
        }

        GeographicLocation? coordinates = null;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var coordinatesResult = GeographicLocation.Create(request.Latitude.Value, request.Longitude.Value);
            if (coordinatesResult.IsFailure)
            {
                return Result.Failure(coordinatesResult.Error);
            }

            coordinates = coordinatesResult.Value;
        }

        var workLocationResult = await OrganizationLookup.LoadWorkLocationForTenantAsync(
            _repository, request.WorkLocationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (workLocationResult.IsFailure)
        {
            return Result.Failure(workLocationResult.Error);
        }

        return workLocationResult.Value.Update(
            request.Name, addressResult.Value, timeZoneResult.Value, coordinates, request.LegalEntityId, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveWorkLocationCommand(Guid WorkLocationId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveWorkLocationCommandHandler : IRequestHandler<ArchiveWorkLocationCommand, Result>
{
    private readonly IWorkLocationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveWorkLocationCommandHandler(IWorkLocationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveWorkLocationCommand request, CancellationToken cancellationToken)
    {
        var workLocationResult = await OrganizationLookup.LoadWorkLocationForTenantAsync(
            _repository, request.WorkLocationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return workLocationResult.IsFailure
            ? Result.Failure(workLocationResult.Error)
            : workLocationResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}

public sealed record RestoreWorkLocationCommand(Guid WorkLocationId, Guid TenantId) : ICommand<Result>;

internal sealed class RestoreWorkLocationCommandHandler : IRequestHandler<RestoreWorkLocationCommand, Result>
{
    private readonly IWorkLocationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RestoreWorkLocationCommandHandler(IWorkLocationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RestoreWorkLocationCommand request, CancellationToken cancellationToken)
    {
        var workLocationResult = await OrganizationLookup.LoadWorkLocationForTenantAsync(
            _repository, request.WorkLocationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return workLocationResult.IsFailure
            ? Result.Failure(workLocationResult.Error)
            : workLocationResult.Value.Restore(_timeProvider.GetUtcNow());
    }
}

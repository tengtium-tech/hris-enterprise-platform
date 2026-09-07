using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Legal Entity Aggregate's own lifecycle commands, including LEG-001/LEG-002's own
/// business registration number and tax identification number uniqueness checks.
/// </summary>
public sealed record CreateLegalEntityCommand(
    Guid TenantId,
    string? Code,
    string? Name,
    string? RegisteredBusinessName,
    string? BusinessRegistrationNumber,
    string? TaxIdentificationNumber,
    string? Country,
    string? Currency,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? ProvinceOrState,
    string? PostalCode,
    string? AddressCountry) : ICommand<Result<Guid>>;

internal sealed class CreateLegalEntityCommandHandler : IRequestHandler<CreateLegalEntityCommand, Result<Guid>>
{
    private readonly ILegalEntityRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateLegalEntityCommandHandler(ILegalEntityRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateLegalEntityCommand request, CancellationToken cancellationToken)
    {
        if (request.BusinessRegistrationNumber is not null
            && await _repository.ExistsWithBusinessRegistrationNumberAsync(request.BusinessRegistrationNumber, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateBusinessRegistrationNumber);
        }

        if (request.TaxIdentificationNumber is not null
            && await _repository.ExistsWithTaxIdentificationNumberAsync(request.TaxIdentificationNumber, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateTaxIdentificationNumber);
        }

        TaxIdentificationNumber? taxId = null;
        if (request.TaxIdentificationNumber is not null)
        {
            var taxIdResult = Domain.TaxIdentificationNumber.Create(request.TaxIdentificationNumber);
            if (taxIdResult.IsFailure)
            {
                return Result.Failure<Guid>(taxIdResult.Error);
            }

            taxId = taxIdResult.Value;
        }

        CurrencyCode? currency = null;
        if (request.Currency is not null)
        {
            var currencyResult = CurrencyCode.Create(request.Currency);
            if (currencyResult.IsFailure)
            {
                return Result.Failure<Guid>(currencyResult.Error);
            }

            currency = currencyResult.Value;
        }

        Address? registeredAddress = null;
        if (request.AddressLine1 is not null || request.City is not null || request.AddressCountry is not null)
        {
            var addressResult = Address.Create(
                request.AddressLine1, request.AddressLine2, request.City, request.ProvinceOrState, request.PostalCode,
                request.AddressCountry);
            if (addressResult.IsFailure)
            {
                return Result.Failure<Guid>(addressResult.Error);
            }

            registeredAddress = addressResult.Value;
        }

        var legalEntityResult = LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, request.RegisteredBusinessName,
            request.BusinessRegistrationNumber, taxId, request.Country, currency, registeredAddress, _timeProvider.GetUtcNow());
        if (legalEntityResult.IsFailure)
        {
            return Result.Failure<Guid>(legalEntityResult.Error);
        }

        await _repository.AddAsync(legalEntityResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(legalEntityResult.Value.Id.Value);
    }
}

public sealed record UpdateLegalEntityCommand(
    Guid LegalEntityId,
    Guid TenantId,
    string? Name,
    string? RegisteredBusinessName,
    string? TaxIdentificationNumber,
    string? Country,
    string? Currency,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? ProvinceOrState,
    string? PostalCode,
    string? AddressCountry) : ICommand<Result>;

internal sealed class UpdateLegalEntityCommandHandler : IRequestHandler<UpdateLegalEntityCommand, Result>
{
    private readonly ILegalEntityRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateLegalEntityCommandHandler(ILegalEntityRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateLegalEntityCommand request, CancellationToken cancellationToken)
    {
        if (request.TaxIdentificationNumber is not null
            && await _repository.ExistsWithTaxIdentificationNumberAsync(
                request.TaxIdentificationNumber, new LegalEntityId(request.LegalEntityId), cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure(OrganizationErrors.DuplicateTaxIdentificationNumber);
        }

        TaxIdentificationNumber? taxId = null;
        if (request.TaxIdentificationNumber is not null)
        {
            var taxIdResult = Domain.TaxIdentificationNumber.Create(request.TaxIdentificationNumber);
            if (taxIdResult.IsFailure)
            {
                return Result.Failure(taxIdResult.Error);
            }

            taxId = taxIdResult.Value;
        }

        CurrencyCode? currency = null;
        if (request.Currency is not null)
        {
            var currencyResult = CurrencyCode.Create(request.Currency);
            if (currencyResult.IsFailure)
            {
                return Result.Failure(currencyResult.Error);
            }

            currency = currencyResult.Value;
        }

        Address? registeredAddress = null;
        if (request.AddressLine1 is not null || request.City is not null || request.AddressCountry is not null)
        {
            var addressResult = Address.Create(
                request.AddressLine1, request.AddressLine2, request.City, request.ProvinceOrState, request.PostalCode,
                request.AddressCountry);
            if (addressResult.IsFailure)
            {
                return Result.Failure(addressResult.Error);
            }

            registeredAddress = addressResult.Value;
        }

        var legalEntityResult = await OrganizationLookup.LoadLegalEntityForTenantAsync(
            _repository, request.LegalEntityId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (legalEntityResult.IsFailure)
        {
            return Result.Failure(legalEntityResult.Error);
        }

        return legalEntityResult.Value.Update(
            request.Name, request.RegisteredBusinessName, taxId, request.Country, currency, registeredAddress,
            _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveLegalEntityCommand(Guid LegalEntityId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveLegalEntityCommandHandler : IRequestHandler<ArchiveLegalEntityCommand, Result>
{
    private readonly ILegalEntityRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveLegalEntityCommandHandler(ILegalEntityRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveLegalEntityCommand request, CancellationToken cancellationToken)
    {
        var legalEntityResult = await OrganizationLookup.LoadLegalEntityForTenantAsync(
            _repository, request.LegalEntityId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return legalEntityResult.IsFailure
            ? Result.Failure(legalEntityResult.Error)
            : legalEntityResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}

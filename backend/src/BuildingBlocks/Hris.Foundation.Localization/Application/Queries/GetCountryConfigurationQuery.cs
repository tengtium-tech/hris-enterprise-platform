using Hris.Application.Abstractions;
using Hris.Foundation.Localization.Application.Dtos;
using Hris.Foundation.Localization.Application.Mapping;
using Hris.Foundation.Localization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Localization.Application.Queries;

/// <summary>
/// Reads back one country's own defaults, per localization-framework.md's own
/// Downstream Consumers (Payroll, Time &amp; Attendance, and every other business
/// module named there resolves a country's currency/language/time zone/working days
/// through this). Ungated: reading a platform-wide default is not the "translation
/// management" this document's own Security Considerations names for authorization,
/// and every one of this document's own Downstream Consumers needs unrestricted
/// read access to it to function at all.
/// </summary>
public sealed record GetCountryConfigurationQuery(string Country) : IQuery<Result<CountryConfigurationDto>>;

internal sealed class GetCountryConfigurationQueryHandler
    : IRequestHandler<GetCountryConfigurationQuery, Result<CountryConfigurationDto>>
{
    private readonly ICountryConfigurationRepository _repository;

    public GetCountryConfigurationQueryHandler(ICountryConfigurationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<CountryConfigurationDto>> Handle(
        GetCountryConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var countryResult = CountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure<CountryConfigurationDto>(countryResult.Error);
        }

        var configuration = await _repository.GetByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);

        return configuration is null
            ? Result.Failure<CountryConfigurationDto>(LocalizationErrors.CountryConfigurationNotFound)
            : Result.Success(LocalizationMapper.ToDto(configuration));
    }
}

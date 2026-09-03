using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// Re-checks an already-assigned number against its own series' current format -- see
/// <see cref="IssuedNumber.Validate"/>. Loads and passes the series' own current
/// <see cref="NumberPrefix"/>/<see cref="NumberFormat"/> in explicitly -- the one place
/// this framework's own cross-aggregate validation happens in the handler, never inside
/// either aggregate's own methods, the identical split
/// <c>RegisterHookCommandHandler</c>'s own remarks establish.
/// </summary>
public sealed record ValidateNumberCommand(Guid IssuedNumberId) : ICommand<Result>;

internal sealed class ValidateNumberCommandHandler : IRequestHandler<ValidateNumberCommand, Result>
{
    private readonly IIssuedNumberRepository _issuedNumberRepository;
    private readonly INumberSeriesRepository _numberSeriesRepository;
    private readonly TimeProvider _timeProvider;

    public ValidateNumberCommandHandler(
        IIssuedNumberRepository issuedNumberRepository,
        INumberSeriesRepository numberSeriesRepository,
        TimeProvider timeProvider)
    {
        _issuedNumberRepository = Guard.AgainstNull(issuedNumberRepository, nameof(issuedNumberRepository));
        _numberSeriesRepository = Guard.AgainstNull(numberSeriesRepository, nameof(numberSeriesRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ValidateNumberCommand request, CancellationToken cancellationToken)
    {
        var issuedNumber = await _issuedNumberRepository
            .GetByIdAsync(new IssuedNumberId(request.IssuedNumberId), cancellationToken)
            .ConfigureAwait(false);
        if (issuedNumber is null)
        {
            return Result.Failure(NumberingErrors.IssuedNumberNotFound);
        }

        var numberSeries = await _numberSeriesRepository
            .GetByIdAsync(issuedNumber.NumberSeriesId, cancellationToken)
            .ConfigureAwait(false);
        if (numberSeries is null)
        {
            return Result.Failure(NumberingErrors.NumberSeriesNotFound);
        }

        return issuedNumber.Validate(numberSeries.Prefix, numberSeries.Format, _timeProvider.GetUtcNow());
    }
}

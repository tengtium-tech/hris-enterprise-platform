using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// The common "give me a new number" path -- <see cref="IssuedNumber.Request"/>
/// immediately followed by <see cref="IssuedNumber.Reserve"/>, folded into one command
/// because in practice a caller almost never wants a bare, unreserved
/// <see cref="NumberLifecycleStatus.Requested"/> row: the atomic reservation itself is
/// cheap and immediate, so there is no useful reason to expose that transient state as
/// its own separately-invoked command. The identical collapsing choice
/// <c>StoredFile.MarkStored</c>'s own remarks make for its Stored/Available boundary.
///
/// This handler, not <see cref="IssuedNumber"/> itself, is where
/// <see cref="INumberSeriesRepository.IncrementAndGetNextSequenceValueAsync"/> is
/// called -- the one atomic, non-negotiable step in this whole command, executed before
/// either Domain method runs, so the sequence value <see cref="IssuedNumber.Reserve"/>
/// receives was already safely and uniquely claimed by the time it arrives.
/// </summary>
public sealed record RequestAndReserveNumberCommand(Guid NumberSeriesId) : ICommand<Result<Guid>>;

internal sealed class RequestAndReserveNumberCommandHandler : IRequestHandler<RequestAndReserveNumberCommand, Result<Guid>>
{
    private readonly INumberSeriesRepository _numberSeriesRepository;
    private readonly IIssuedNumberRepository _issuedNumberRepository;
    private readonly TimeProvider _timeProvider;

    public RequestAndReserveNumberCommandHandler(
        INumberSeriesRepository numberSeriesRepository,
        IIssuedNumberRepository issuedNumberRepository,
        TimeProvider timeProvider)
    {
        _numberSeriesRepository = Guard.AgainstNull(numberSeriesRepository, nameof(numberSeriesRepository));
        _issuedNumberRepository = Guard.AgainstNull(issuedNumberRepository, nameof(issuedNumberRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RequestAndReserveNumberCommand request, CancellationToken cancellationToken)
    {
        var seriesId = new NumberSeriesId(request.NumberSeriesId);

        var numberSeries = await _numberSeriesRepository.GetByIdAsync(seriesId, cancellationToken).ConfigureAwait(false);
        if (numberSeries is null)
        {
            return Result.Failure<Guid>(NumberingErrors.NumberSeriesNotFound);
        }

        var nowUtc = _timeProvider.GetUtcNow();

        var requestResult = IssuedNumber.Request(seriesId, nowUtc);
        if (requestResult.IsFailure)
        {
            return Result.Failure<Guid>(requestResult.Error);
        }

        var issuedNumber = requestResult.Value;

        var sequenceValue = await _numberSeriesRepository
            .IncrementAndGetNextSequenceValueAsync(seriesId, cancellationToken)
            .ConfigureAwait(false);

        numberSeries.ReconcileSequenceValueAfterAtomicIncrement(sequenceValue);

        var formattedValue = numberSeries.Format.Format(numberSeries.Prefix, sequenceValue, nowUtc);
        var formattedNumberResult = FormattedNumber.Create(formattedValue);
        if (formattedNumberResult.IsFailure)
        {
            return Result.Failure<Guid>(formattedNumberResult.Error);
        }

        var reserveResult = issuedNumber.Reserve(sequenceValue, formattedNumberResult.Value, nowUtc);
        if (reserveResult.IsFailure)
        {
            return Result.Failure<Guid>(reserveResult.Error);
        }

        await _issuedNumberRepository.AddAsync(issuedNumber, cancellationToken).ConfigureAwait(false);

        return Result.Success(issuedNumber.Id.Value);
    }
}

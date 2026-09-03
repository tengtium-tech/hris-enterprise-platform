using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// The two remaining <see cref="NumberSeries"/>-level operations -- update its own
/// format configuration, and force a sequence reset -- grouped into one file the same
/// way every other Sprint 3/4 framework's own bundled lifecycle commands are.
/// </summary>
public sealed record UpdateNumberSeriesFormatCommand(
    Guid NumberSeriesId,
    string Prefix,
    int RunningNumberLength,
    bool IncludeYear,
    bool IncludeMonth,
    string Separator,
    SequenceResetPolicy ResetPolicy) : ICommand<Result>;

internal sealed class UpdateNumberSeriesFormatCommandHandler : IRequestHandler<UpdateNumberSeriesFormatCommand, Result>
{
    private readonly INumberSeriesRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateNumberSeriesFormatCommandHandler(INumberSeriesRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateNumberSeriesFormatCommand request, CancellationToken cancellationToken)
    {
        var numberSeries = await _repository.GetByIdAsync(new NumberSeriesId(request.NumberSeriesId), cancellationToken).ConfigureAwait(false);
        if (numberSeries is null)
        {
            return Result.Failure(NumberingErrors.NumberSeriesNotFound);
        }

        var prefixResult = NumberPrefix.Create(request.Prefix);
        if (prefixResult.IsFailure)
        {
            return Result.Failure(prefixResult.Error);
        }

        var formatResult = NumberFormat.Create(request.RunningNumberLength, request.IncludeYear, request.IncludeMonth, request.Separator);
        if (formatResult.IsFailure)
        {
            return Result.Failure(formatResult.Error);
        }

        return numberSeries.UpdateFormat(prefixResult.Value, formatResult.Value, request.ResetPolicy, _timeProvider.GetUtcNow());
    }
}

public sealed record ResetSequenceCommand(Guid NumberSeriesId) : ICommand<Result>;

internal sealed class ResetSequenceCommandHandler : IRequestHandler<ResetSequenceCommand, Result>
{
    private readonly INumberSeriesRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ResetSequenceCommandHandler(INumberSeriesRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ResetSequenceCommand request, CancellationToken cancellationToken)
    {
        var numberSeries = await _repository.GetByIdAsync(new NumberSeriesId(request.NumberSeriesId), cancellationToken).ConfigureAwait(false);
        if (numberSeries is null)
        {
            return Result.Failure(NumberingErrors.NumberSeriesNotFound);
        }

        return numberSeries.ResetSequence(_timeProvider.GetUtcNow());
    }
}

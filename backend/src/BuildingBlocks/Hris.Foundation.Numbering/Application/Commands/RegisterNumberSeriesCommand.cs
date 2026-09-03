using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// Registers a new Number Series. Carries raw primitives, not Domain Value Objects,
/// across the MediatR boundary -- <see cref="RegisterNumberSeriesCommandHandler"/> is
/// the one place a malformed key, prefix, or format becomes a
/// <see cref="NumberingErrors"/> failure.
/// </summary>
public sealed record RegisterNumberSeriesCommand(
    string Key,
    string Prefix,
    int RunningNumberLength,
    bool IncludeYear,
    bool IncludeMonth,
    string Separator,
    SequenceResetPolicy ResetPolicy) : ICommand<Result<Guid>>;

internal sealed class RegisterNumberSeriesCommandHandler : IRequestHandler<RegisterNumberSeriesCommand, Result<Guid>>
{
    private readonly INumberSeriesRepository _repository;

    public RegisterNumberSeriesCommandHandler(INumberSeriesRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<Guid>> Handle(RegisterNumberSeriesCommand request, CancellationToken cancellationToken)
    {
        var keyResult = SeriesKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<Guid>(keyResult.Error);
        }

        if (await _repository.ExistsByKeyAsync(keyResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(NumberingErrors.SeriesKeyAlreadyRegistered);
        }

        var prefixResult = NumberPrefix.Create(request.Prefix);
        if (prefixResult.IsFailure)
        {
            return Result.Failure<Guid>(prefixResult.Error);
        }

        var formatResult = NumberFormat.Create(request.RunningNumberLength, request.IncludeYear, request.IncludeMonth, request.Separator);
        if (formatResult.IsFailure)
        {
            return Result.Failure<Guid>(formatResult.Error);
        }

        var registrationResult = NumberSeries.Register(keyResult.Value, prefixResult.Value, formatResult.Value, request.ResetPolicy);
        if (registrationResult.IsFailure)
        {
            return Result.Failure<Guid>(registrationResult.Error);
        }

        await _repository.AddAsync(registrationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registrationResult.Value.Id.Value);
    }
}

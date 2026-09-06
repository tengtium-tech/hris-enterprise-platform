using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Commands;

/// <summary>
/// The four remaining <see cref="IntegrationRun"/> lifecycle transitions -- Complete,
/// Fail, Retry, MoveToDeadLetter -- grouped into one file the same way every other
/// framework's own bundled lifecycle commands are.
/// </summary>
public sealed record CompleteIntegrationRunCommand(Guid IntegrationRunId, Guid TenantId, int RecordsProcessed) : ICommand<Result>;

internal sealed class CompleteIntegrationRunCommandHandler : IRequestHandler<CompleteIntegrationRunCommand, Result>
{
    private readonly IIntegrationRunRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteIntegrationRunCommandHandler(IIntegrationRunRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CompleteIntegrationRunCommand request, CancellationToken cancellationToken)
    {
        var runResult = await IntegrationLookup.LoadIntegrationRunForTenantAsync(
            _repository, request.IntegrationRunId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return runResult.IsFailure
            ? Result.Failure(runResult.Error)
            : runResult.Value.Complete(request.RecordsProcessed, _timeProvider.GetUtcNow());
    }
}

public sealed record FailIntegrationRunCommand(Guid IntegrationRunId, Guid TenantId, string Reason) : ICommand<Result>;

internal sealed class FailIntegrationRunCommandHandler : IRequestHandler<FailIntegrationRunCommand, Result>
{
    private readonly IIntegrationRunRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FailIntegrationRunCommandHandler(IIntegrationRunRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FailIntegrationRunCommand request, CancellationToken cancellationToken)
    {
        var runResult = await IntegrationLookup.LoadIntegrationRunForTenantAsync(
            _repository, request.IntegrationRunId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return runResult.IsFailure ? Result.Failure(runResult.Error) : runResult.Value.Fail(request.Reason, _timeProvider.GetUtcNow());
    }
}

public sealed record RetryIntegrationRunCommand(Guid IntegrationRunId, Guid TenantId) : ICommand<Result>;

internal sealed class RetryIntegrationRunCommandHandler : IRequestHandler<RetryIntegrationRunCommand, Result>
{
    private readonly IIntegrationRunRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetryIntegrationRunCommandHandler(IIntegrationRunRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetryIntegrationRunCommand request, CancellationToken cancellationToken)
    {
        var runResult = await IntegrationLookup.LoadIntegrationRunForTenantAsync(
            _repository, request.IntegrationRunId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return runResult.IsFailure ? Result.Failure(runResult.Error) : runResult.Value.Retry(_timeProvider.GetUtcNow());
    }
}

public sealed record MoveIntegrationRunToDeadLetterCommand(Guid IntegrationRunId, Guid TenantId) : ICommand<Result>;

internal sealed class MoveIntegrationRunToDeadLetterCommandHandler : IRequestHandler<MoveIntegrationRunToDeadLetterCommand, Result>
{
    private readonly IIntegrationRunRepository _repository;

    public MoveIntegrationRunToDeadLetterCommandHandler(IIntegrationRunRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(MoveIntegrationRunToDeadLetterCommand request, CancellationToken cancellationToken)
    {
        var runResult = await IntegrationLookup.LoadIntegrationRunForTenantAsync(
            _repository, request.IntegrationRunId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return runResult.IsFailure ? Result.Failure(runResult.Error) : runResult.Value.MoveToDeadLetter();
    }
}

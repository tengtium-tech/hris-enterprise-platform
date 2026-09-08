using Hris.Application.Abstractions;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Commands;

/// <summary>
/// Every Lifecycle Stage transition command on <see cref="Domain.Employee"/>
/// follows the same shape: load, record the previous stage, call the one
/// corresponding Aggregate method, append an <see cref="EmployeeHistory"/> record
/// of the transition. Split into one record + handler pair per command rather than
/// one generic "transition" command, matching commands.md's own "one business
/// intention per command" principle and <c>Employment</c> module's own precedent of
/// separate Suspend/Reinstate/Second commands rather than a single parameterized one.
/// </summary>
public sealed record StartEmployeeOnboardingCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class StartEmployeeOnboardingCommandHandler : IRequestHandler<StartEmployeeOnboardingCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public StartEmployeeOnboardingCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(StartEmployeeOnboardingCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.StartOnboarding, cancellationToken).ConfigureAwait(false);
}

public sealed record ActivateEmployeeCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateEmployeeCommandHandler : IRequestHandler<ActivateEmployeeCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public ActivateEmployeeCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateEmployeeCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.Activate, cancellationToken).ConfigureAwait(false);
}

public sealed record StartEmployeeOffboardingCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class StartEmployeeOffboardingCommandHandler : IRequestHandler<StartEmployeeOffboardingCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public StartEmployeeOffboardingCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(StartEmployeeOffboardingCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.StartOffboarding, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Reaches <see cref="EmployeeLifecycleStage.Separated"/>. See
/// <c>Domain.Employee.Separate</c>'s own remarks on the deferred automatic
/// "no Employment remains active" trigger.
/// </summary>
public sealed record SeparateEmployeeCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class SeparateEmployeeCommandHandler : IRequestHandler<SeparateEmployeeCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public SeparateEmployeeCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SeparateEmployeeCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.Separate, cancellationToken).ConfigureAwait(false);
}

public sealed record RetireEmployeeCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class RetireEmployeeCommandHandler : IRequestHandler<RetireEmployeeCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public RetireEmployeeCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireEmployeeCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.Retire, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Reactivates a formerly Separated Employee (Separated -&gt; Active directly).
/// commands.md's own RehireEmployeeCommand: "Creates a new employment relationship
/// for a former employee" -- creating the new Employment itself is a separate call
/// to the Employment module's own <c>CreateEmploymentCommand</c> by the
/// orchestrating caller (e.g. an API endpoint invoking both commands), never a
/// compile-time reference from here.
/// </summary>
public sealed record RehireEmployeeCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class RehireEmployeeCommandHandler : IRequestHandler<RehireEmployeeCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public RehireEmployeeCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RehireEmployeeCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.Rehire, cancellationToken).ConfigureAwait(false);
}

public sealed record RecordEmployeeDeceasedCommand(Guid EmployeeId, Guid TenantId) : ICommand<Result>;

internal sealed class RecordEmployeeDeceasedCommandHandler : IRequestHandler<RecordEmployeeDeceasedCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public RecordEmployeeDeceasedCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordEmployeeDeceasedCommand request, CancellationToken cancellationToken) =>
        await EmployeeLifecycleTransitions.TransitionAsync(
            _repository, _historyRepository, _timeProvider, request.EmployeeId, request.TenantId,
            employee => employee.RecordDeceased, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Shared transition-plus-history-recording logic every command handler in this
/// file delegates to, matching <c>Employment</c> module's own
/// <c>ChangePositionCommandHandler</c> static-helper precedent for consolidating
/// near-identical handler bodies.
/// </summary>
internal static class EmployeeLifecycleTransitions
{
    public static async Task<Result> TransitionAsync(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider,
        Guid employeeId, Guid tenantId, Func<Domain.Employee, Func<DateTimeOffset, Result>> transitionSelector,
        CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(repository, employeeId, tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure(employeeResult.Error);
        }

        var previousStage = employeeResult.Value.LifecycleStage;
        var nowUtc = timeProvider.GetUtcNow();
        var transition = transitionSelector(employeeResult.Value);
        var result = transition(nowUtc);
        if (result.IsFailure)
        {
            return result;
        }

        await EmployeeHistoryRecorder.RecordAsync(
            historyRepository, tenantId, employeeId, EmployeeHistoryCategory.LifecycleStage, previousStage.ToString(),
            employeeResult.Value.LifecycleStage.ToString(), nowUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

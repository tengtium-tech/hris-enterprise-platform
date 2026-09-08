using Hris.Application.Abstractions;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Commands;

/// <summary>
/// Creates a new Employment. When <paramref name="IsPrimary"/> is
/// <see langword="false"/> this is a Concurrent Employment creation
/// (CreateConcurrentEmploymentCommand in commands.md is not a separate command
/// class here -- the same aggregate factory method and the same tenant-enablement
/// check apply regardless of caller). When <paramref name="PriorEmploymentId"/> is
/// supplied this is a rehire (RehireEmployeeCommand in commands.md), also folded
/// into this one command rather than duplicated, since the only difference is one
/// extra caller-supplied identifier.
/// </summary>
public sealed record CreateEmploymentCommand(
    Guid TenantId,
    Guid EmployeeId,
    string? Number,
    string? EmploymentType,
    string? Category,
    bool IsPrimary,
    Guid? PrimaryEmploymentId,
    Guid? PriorEmploymentId) : ICommand<Result<Guid>>;

internal sealed class CreateEmploymentCommandHandler : IRequestHandler<CreateEmploymentCommand, Result<Guid>>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateEmploymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Number is not null
            && await _repository.ExistsWithNumberAsync(request.TenantId, request.Number, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(EmploymentErrors.DuplicateEmploymentNumber);
        }

        var existingPrimary = await _repository
            .GetActivePrimaryEmploymentAsync(request.TenantId, request.EmployeeId, cancellationToken)
            .ConfigureAwait(false);

        // No concurrent-employment tenant-policy source exists yet (Rules Engine
        // integration deferred, per employment-policies.md's own Policy-vs-Invariant
        // distinction); "enabled" is inferred from context: creating a second active
        // Employment for an Employee, or explicitly creating one as non-primary,
        // both require it. Documented as a scope decision, not an invented policy
        // store.
        var concurrentEmploymentEnabled = !request.IsPrimary || existingPrimary is null || request.PriorEmploymentId.HasValue;

        var id = new EmploymentId(Guid.NewGuid());
        var nowUtc = _timeProvider.GetUtcNow();

        var employmentResult = Domain.Employment.Create(
            id, request.TenantId, request.EmployeeId, request.Number, request.EmploymentType, request.Category,
            request.IsPrimary, request.PrimaryEmploymentId, request.PriorEmploymentId, concurrentEmploymentEnabled,
            existingPrimary is not null, nowUtc);
        if (employmentResult.IsFailure)
        {
            return Result.Failure<Guid>(employmentResult.Error);
        }

        await _repository.AddAsync(employmentResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(employmentResult.Value.Id.Value);
    }
}

public sealed record ActivateEmploymentCommand(Guid EmploymentId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateEmploymentCommandHandler : IRequestHandler<ActivateEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _employmentRepository;
    private readonly IEmploymentContractRepository _contractRepository;
    private readonly IEmploymentAssignmentRepository _assignmentRepository;
    private readonly TimeProvider _timeProvider;

    public ActivateEmploymentCommandHandler(
        IEmploymentRepository employmentRepository, IEmploymentContractRepository contractRepository,
        IEmploymentAssignmentRepository assignmentRepository, TimeProvider timeProvider)
    {
        _employmentRepository = Guard.AgainstNull(employmentRepository, nameof(employmentRepository));
        _contractRepository = Guard.AgainstNull(contractRepository, nameof(contractRepository));
        _assignmentRepository = Guard.AgainstNull(assignmentRepository, nameof(assignmentRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _employmentRepository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employmentResult.IsFailure)
        {
            return Result.Failure(employmentResult.Error);
        }

        var hasValidContract = await _contractRepository
            .HasValidContractAsync(request.TenantId, request.EmploymentId, cancellationToken).ConfigureAwait(false);
        var hasValidAssignment = await _assignmentRepository
            .HasValidAssignmentAsync(request.TenantId, request.EmploymentId, cancellationToken).ConfigureAwait(false);

        return employmentResult.Value.Activate(hasValidContract, hasValidAssignment, _timeProvider.GetUtcNow());
    }
}

public sealed record ChangeEmploymentTypeCommand(Guid EmploymentId, Guid TenantId, string? NewType) : ICommand<Result>;

internal sealed class ChangeEmploymentTypeCommandHandler : IRequestHandler<ChangeEmploymentTypeCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeEmploymentTypeCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeEmploymentTypeCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.ChangeEmploymentType(request.NewType, _timeProvider.GetUtcNow());
    }
}

public sealed record ChangeEmploymentCategoryCommand(Guid EmploymentId, Guid TenantId, string? NewCategory) : ICommand<Result>;

internal sealed class ChangeEmploymentCategoryCommandHandler : IRequestHandler<ChangeEmploymentCategoryCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeEmploymentCategoryCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeEmploymentCategoryCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.ChangeEmploymentCategory(request.NewCategory, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Designates <see cref="EmploymentId"/> Primary, demoting the Employee's current
/// Primary Employment to secondary. Orchestrates two <see cref="Domain.Employment"/>
/// instances within one command -- an accepted exception to "one transaction, one
/// Aggregate Root" for this specific coordinated pair, matching
/// infrastructure/persistence.md's own "cross-Aggregate workflows are coordinated by
/// the Application layer" guidance; both saves commit together under the same
/// <c>DbContext.SaveChanges</c> call.
/// </summary>
public sealed record ChangePrimaryEmploymentCommand(Guid EmploymentId, Guid TenantId) : ICommand<Result>;

internal sealed class ChangePrimaryEmploymentCommandHandler : IRequestHandler<ChangePrimaryEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangePrimaryEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangePrimaryEmploymentCommand request, CancellationToken cancellationToken)
    {
        var newPrimaryResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (newPrimaryResult.IsFailure)
        {
            return Result.Failure(newPrimaryResult.Error);
        }

        var currentPrimary = await _repository
            .GetActivePrimaryEmploymentAsync(request.TenantId, newPrimaryResult.Value.EmployeeId, cancellationToken)
            .ConfigureAwait(false);
        if (currentPrimary is null)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotFound);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var markPrimaryResult = newPrimaryResult.Value.MarkPrimary(currentPrimary.Id.Value, nowUtc);
        if (markPrimaryResult.IsFailure)
        {
            return markPrimaryResult;
        }

        currentPrimary.MarkSecondary();
        return Result.Success();
    }
}

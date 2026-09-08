using Hris.Application.Abstractions;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Commands;

public sealed record CreateEmploymentContractCommand(
    Guid EmploymentId,
    Guid TenantId,
    string? ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsFixedTerm,
    Guid? SupersedesContractId) : ICommand<Result<Guid>>;

internal sealed class CreateEmploymentContractCommandHandler
    : IRequestHandler<CreateEmploymentContractCommand, Result<Guid>>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var id = new EmploymentContractId(Guid.NewGuid());
        var contractResult = EmploymentContract.Create(
            id, request.TenantId, request.EmploymentId, request.ContractType, request.StartDate, request.EndDate,
            request.IsFixedTerm, request.SupersedesContractId, _timeProvider.GetUtcNow());
        if (contractResult.IsFailure)
        {
            return Result.Failure<Guid>(contractResult.Error);
        }

        await _repository.AddAsync(contractResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(contractResult.Value.Id.Value);
    }
}

public sealed record ApproveEmploymentContractCommand(Guid EmploymentContractId, Guid TenantId) : ICommand<Result>;

internal sealed class ApproveEmploymentContractCommandHandler : IRequestHandler<ApproveEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure ? Result.Failure(contractResult.Error) : contractResult.Value.Approve(_timeProvider.GetUtcNow());
    }
}

public sealed record ActivateEmploymentContractCommand(Guid EmploymentContractId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateEmploymentContractCommandHandler : IRequestHandler<ActivateEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.MakeEffective(_timeProvider.GetUtcNow());
    }
}

public sealed record RenewEmploymentContractCommand(
    Guid EmploymentContractId, Guid TenantId, DateOnly NewStartDate, DateOnly? NewEndDate, string? ApprovalReference)
    : ICommand<Result>;

internal sealed class RenewEmploymentContractCommandHandler : IRequestHandler<RenewEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenewEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenewEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.Renew(
                request.NewStartDate, request.NewEndDate, request.ApprovalReference, _timeProvider.GetUtcNow());
    }
}

public sealed record ExtendEmploymentContractCommand(Guid EmploymentContractId, Guid TenantId, DateOnly NewEndDate, string? Reason)
    : ICommand<Result>;

internal sealed class ExtendEmploymentContractCommandHandler : IRequestHandler<ExtendEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExtendEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExtendEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.Extend(request.NewEndDate, request.Reason, _timeProvider.GetUtcNow());
    }
}

public sealed record SupersedeEmploymentContractCommand(Guid EmploymentContractId, Guid TenantId) : ICommand<Result>;

internal sealed class SupersedeEmploymentContractCommandHandler : IRequestHandler<SupersedeEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SupersedeEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SupersedeEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.Supersede(_timeProvider.GetUtcNow());
    }
}

public sealed record CloseEmploymentContractCommand(Guid EmploymentContractId, Guid TenantId) : ICommand<Result>;

internal sealed class CloseEmploymentContractCommandHandler : IRequestHandler<CloseEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CloseEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CloseEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure ? Result.Failure(contractResult.Error) : contractResult.Value.Close(_timeProvider.GetUtcNow());
    }
}

public sealed record CancelEmploymentContractCommand(Guid EmploymentContractId, Guid TenantId) : ICommand<Result>;

internal sealed class CancelEmploymentContractCommandHandler : IRequestHandler<CancelEmploymentContractCommand, Result>
{
    private readonly IEmploymentContractRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelEmploymentContractCommandHandler(IEmploymentContractRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelEmploymentContractCommand request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.Cancel(_timeProvider.GetUtcNow());
    }
}

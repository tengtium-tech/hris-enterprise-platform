using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Commands;

/// <summary>
/// The four remaining Platform-Operator-only lifecycle transitions over
/// <see cref="Domain.Tenant"/> -- Suspend, Reactivate, Archive, Delete -- grouped into
/// one file the same way Localization Framework's own five update commands are
/// bundled in <c>CountryConfigurationUpdateCommands.cs</c>: each handler here is the
/// same shape -- look the aggregate up by <see cref="TenantId"/>, fail with
/// <see cref="TenantErrors.TenantNotFound"/> if it does not exist, otherwise call the
/// one Domain method and return its own <see cref="Result"/>. None needs an explicit
/// save: the aggregate was already loaded through this same <c>DbContext</c>, so the
/// caller's own <c>TransactionBehavior</c> persists the mutation via change tracking
/// alone.
/// </summary>
public sealed record SuspendTenantCommand(Guid TenantId, string Reason, Guid SuspendedBy) : ICommand<Result>;

internal sealed class SuspendTenantCommandHandler : IRequestHandler<SuspendTenantCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SuspendTenantCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.Suspend(request.Reason, new PlatformOperatorId(request.SuspendedBy), _timeProvider.GetUtcNow());
    }
}

public sealed record ReactivateTenantCommand(Guid TenantId, Guid ReactivatedBy) : ICommand<Result>;

internal sealed class ReactivateTenantCommandHandler : IRequestHandler<ReactivateTenantCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReactivateTenantCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.Reactivate(new PlatformOperatorId(request.ReactivatedBy), _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveTenantCommand(Guid TenantId, string Reason, Guid ArchivedBy) : ICommand<Result>;

internal sealed class ArchiveTenantCommandHandler : IRequestHandler<ArchiveTenantCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveTenantCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.Archive(request.Reason, new PlatformOperatorId(request.ArchivedBy), _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// <see cref="ComplianceBasis"/> and the retention-gate check itself (Archived-only,
/// enforced inside <see cref="Domain.Tenant.Delete"/>) are this command's own two
/// invariants; no minimum Archived duration is enforced here, per
/// tenant-framework.md's own deliberately-left-open retention-gate note.
/// </summary>
public sealed record DeleteTenantCommand(Guid TenantId, string Reason, string ComplianceBasis, Guid DeletedBy) : ICommand<Result>;

internal sealed class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeleteTenantCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.Delete(request.Reason, request.ComplianceBasis, new PlatformOperatorId(request.DeletedBy), _timeProvider.GetUtcNow());
    }
}

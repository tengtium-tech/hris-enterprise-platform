using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Employment.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IEmploymentRepository"/>. See
/// <c>PositionRepository</c>'s own remarks for the shared "no explicit
/// <c>UpdateAsync</c>" and "compare against the constructed Value Object" reasoning.
/// </summary>
internal sealed class EmploymentRepository : IEmploymentRepository
{
    private readonly HrisDbContext _dbContext;

    public EmploymentRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Domain.Employment?> GetByIdAsync(EmploymentId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Domain.Employment>().FirstOrDefaultAsync(employment => employment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Employment>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Domain.Employment>()
            .Where(employment => employment.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Domain.Employment>> ListByEmployeeIdAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Domain.Employment>()
            .Where(employment => employment.TenantId == tenantId && employment.EmployeeId == employeeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNumberAsync(
        Guid tenantId, string number, EmploymentId? excludeId, CancellationToken cancellationToken)
    {
        var numberResult = EmploymentNumber.Create(number);
        if (numberResult.IsFailure)
        {
            return false;
        }

        var normalizedNumber = numberResult.Value;
        return await _dbContext.Set<Domain.Employment>()
            .AnyAsync(
                employment => employment.TenantId == tenantId && employment.Number == normalizedNumber
                    && (excludeId == null || employment.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<Domain.Employment?> GetActivePrimaryEmploymentAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Domain.Employment>()
            .FirstOrDefaultAsync(
                employment => employment.TenantId == tenantId && employment.EmployeeId == employeeId
                    && employment.IsPrimary && employment.LifecycleStage != EmploymentLifecycleStage.Separated,
                cancellationToken);
    }

    public async Task AddAsync(Domain.Employment employment, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(employment, nameof(employment));
        await _dbContext.Set<Domain.Employment>().AddAsync(employment, cancellationToken).ConfigureAwait(false);
    }
}

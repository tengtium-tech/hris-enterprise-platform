using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Modules.Employee.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IEmployeeRepository"/>. See
/// <c>EmploymentRepository</c>'s own remarks for the shared "no explicit
/// <c>UpdateAsync</c>" and "compare against the constructed Value Object" reasoning.
/// </summary>
internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly HrisDbContext _dbContext;

    public EmployeeRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Domain.Employee?> GetByIdAsync(EmployeeId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Domain.Employee>().FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Employee>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Domain.Employee>()
            .Where(employee => employee.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNumberAsync(
        Guid tenantId, string number, EmployeeId? excludeId, CancellationToken cancellationToken)
    {
        var numberResult = EmployeeNumber.Create(number);
        if (numberResult.IsFailure)
        {
            return false;
        }

        var normalizedNumber = numberResult.Value;
        return await _dbContext.Set<Domain.Employee>()
            .AnyAsync(
                employee => employee.TenantId == tenantId && employee.Number == normalizedNumber
                    && (excludeId == null || employee.Id != excludeId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Domain.Employee employee, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(employee, nameof(employee));
        await _dbContext.Set<Domain.Employee>().AddAsync(employee, cancellationToken).ConfigureAwait(false);
    }
}

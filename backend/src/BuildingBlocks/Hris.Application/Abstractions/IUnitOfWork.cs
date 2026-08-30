namespace Hris.Application.Abstractions;

/// <summary>
/// The Application layer's own abstraction over dbcontext-design.md's "DbContext =
/// Unit of Work" -- this interface, not <c>HrisDbContext</c> itself, is what
/// <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/> depends on, so this
/// project never takes a compile-time reference to Entity Framework Core. Implemented
/// by <c>Hris.Infrastructure.Persistence.HrisDbContext</c>.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Testing.TenantIsolation;

/// <summary>
/// Per-test scope and transaction rollback, per docs/09-testing/unit-and-integration-testing.md §4.
/// Every tenant-isolation test class inherits this base; the fixture is shared at the class level
/// (one PostgreSQL container per class), while each test method gets its own scope and transaction.
///
/// Declares <see cref="IClassFixture{TFixture}"/> here rather than on each derived class -- xUnit's
/// fixture discovery walks the full inherited interface set, so every future tenant-isolation test
/// class built on this base picks up the fixture source automatically instead of each one needing
/// to redeclare it.
/// </summary>
public abstract class TenantIsolationTestBase : IAsyncLifetime, IClassFixture<TenantIsolationFixture>
{
    private readonly TenantIsolationFixture _fixture;
    private AsyncServiceScope? _scope;
    private IDbContextTransaction? _transaction;

    protected IServiceProvider Services => _scope?.ServiceProvider ?? throw new InvalidOperationException("Scope not initialized.");

    protected HrisDbContext DbContext => Services.GetRequiredService<HrisDbContext>();

    protected TenantIsolationTestBase(TenantIsolationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _scope = _fixture.CreateScope();
        _transaction = await DbContext.Database.BeginTransactionAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }

        if (_scope is not null)
        {
            await _scope.Value.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected async Task SeedAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    protected T GetService<T>() where T : notnull
        => Services.GetRequiredService<T>();
}

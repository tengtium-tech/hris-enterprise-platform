using Hris.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hris.Infrastructure.Persistence;

/// <summary>
/// The platform's one application <c>DbContext</c> for the Modular Monolith phase,
/// per dbcontext-design.md's Context Organization section: "The HRIS Platform uses one
/// application DbContext... Example: HrisDbContext... Business modules remain
/// logically isolated through: Aggregate boundaries, Repositories, Fluent API
/// configuration, Module folders" -- not through separate DbContext types.
///
/// Exposes no <c>DbSet</c> properties directly. Every Aggregate Root's own
/// Infrastructure layer configures its <c>DbSet</c> through its own
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> class (dbcontext-design.md: "Every
/// Aggregate Root should expose one DbSet... Child Entities and Value Objects should
/// not expose independent DbSet properties"); <see cref="ModelCreating"/> discovers
/// those classes by scanning <see cref="PersistenceAssemblyRegistry"/>'s assemblies
/// rather than this context enumerating them, so adding a new framework's or module's
/// persistence never requires editing this file (the same "no inline mapping
/// configuration" requirement dbcontext-design.md states directly: "The DbContext
/// should contain no inline mapping configuration").
///
/// Deliberately does not yet implement dbcontext-design.md's Auditing section
/// (automatic CreatedAt/CreatedBy/ModifiedAt/ModifiedBy population via an EF Core
/// SaveChanges interceptor): populating "By" requires knowing the current actor, which
/// requires Identity Framework's own Infrastructure layer (an HTTP-context-backed
/// current-user accessor) -- not yet built at this point in Sprint 3's bootstrap order
/// (IMPLEMENTATION-PLAN.md: Configuration and Logging first, Identity third). Add the
/// interceptor once that accessor exists; do not invent a placeholder actor now.
/// </summary>
public sealed class HrisDbContext : DbContext, IUnitOfWork
{
    public HrisDbContext(DbContextOptions<HrisDbContext> options)
        : base(options)
    {
    }

    // IUnitOfWork.SaveChangesAsync is satisfied by DbContext's own SaveChangesAsync
    // (inherited, same signature) -- dbcontext-design.md's "DbContext = Unit of Work"
    // standard means this is a genuine implementation, not a forwarding shim.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        foreach (var assembly in PersistenceAssemblyRegistry.Assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}

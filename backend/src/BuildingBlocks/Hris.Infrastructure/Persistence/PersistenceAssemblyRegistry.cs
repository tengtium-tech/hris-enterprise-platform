using System.Collections.Concurrent;
using System.Reflection;

namespace Hris.Infrastructure.Persistence;

/// <summary>
/// The concrete answer to how <see cref="HrisDbContext"/> discovers every Foundation
/// framework's and business module's own <c>IEntityTypeConfiguration&lt;T&gt;</c>
/// classes "through assembly scanning" without this project ever referencing those
/// frameworks/modules directly (dbcontext-design.md, "Model Configuration"; see this
/// project's own .csproj header for why that dependency direction matters -- CTR-ARC-002).
///
/// Populated once, at composition-root time, before <see cref="HrisDbContext"/> is ever
/// resolved: each Foundation framework's own <c>AddXFramework(...)</c> registration
/// method (module-registration.md's "Module Entry Point") calls
/// <see cref="Register"/> with its own assembly, in the same call that registers its
/// MediatR handlers and FluentValidation validators from that assembly. Registration
/// order matches module-registration.md's own stated flow: <c>AddFoundation()</c>
/// (which populates this registry) runs before <c>AddInfrastructure()</c> (which reads
/// it to build <see cref="HrisDbContext"/>'s model).
///
/// A plain static list, not a DI-resolved service: it must be fully populated before
/// the first <see cref="HrisDbContext"/> is constructed, and constructing it through DI
/// would only reintroduce the same ordering requirement one layer up. Registration is
/// idempotent (<see cref="ConcurrentDictionary{TKey,TValue}"/>'s own key semantics) so a
/// framework/module registered twice -- e.g. once by a test host, once by the real one
/// -- does not duplicate model configuration.
///
/// Backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>, not a plain
/// <see cref="HashSet{T}"/>, because this static registry is genuinely written from
/// more than one thread: <c>Hris.Infrastructure.IntegrationTests</c>' own
/// <c>PostgresContainerFixture</c> is instantiated once per xUnit test class using it,
/// and xUnit runs test classes in separate collections in parallel by default -- once a
/// second fixture-using test class existed
/// (<c>NumberSeriesConcurrencyTests</c>, alongside the pre-existing
/// <c>RepositoryQueryTranslationTests</c>), two fixtures' own <c>InitializeAsync</c>
/// began calling every <c>AddXFramework()</c>'s own <see cref="Register"/> concurrently
/// for the first time, corrupting a plain <see cref="HashSet{T}"/>'s internal state --
/// found empirically by that exact failure, not anticipated in advance. A single
/// production host still only ever calls <see cref="Register"/> from one thread at
/// startup, so this fix costs that path nothing.
/// </summary>
public static class PersistenceAssemblyRegistry
{
    private static readonly ConcurrentDictionary<Assembly, byte> _assemblies = new();

    public static IReadOnlyCollection<Assembly> Assemblies => _assemblies.Keys.ToList();

    public static void Register(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.TryAdd(assembly, 0);
    }
}

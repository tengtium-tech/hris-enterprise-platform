using Hris.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Application;

/// <summary>
/// Registers the two pipeline behaviors this project defines, per
/// application-pipeline.md's Pipeline Behaviors order ("Logging, Performance
/// Monitoring, Authorization, Validation, Transaction, Handler, Domain Events").
/// Registration order below is deliberate: MediatR runs pipeline behaviors in
/// registration order, and Validation must reject a malformed request before
/// Transaction ever calls <c>SaveChangesAsync</c> for it.
///
/// Logging, Performance Monitoring, and Authorization behaviors are not registered
/// here -- each depends on a Foundation framework (Logging, Authorization) whose own
/// Infrastructure layer does not exist yet at this point in Sprint 3's bootstrap order.
/// Add them here once those frameworks reach their own Infrastructure layer; do not
/// invent a placeholder logging/authorization behavior now.
///
/// Called once, from <c>Hris.Api</c>'s <c>Program.cs</c>, before any framework's own
/// <c>AddXFramework(...)</c> registers its MediatR handlers -- behavior registration
/// order relative to handler registration does not matter to MediatR, but keeping this
/// call alongside <c>AddMediatR(...)</c>'s own assembly registration keeps the
/// composition root's pipeline wiring in one place.
///
/// Named <see cref="ServiceCollectionExtensions"/> rather than the file's own
/// <c>DependencyInjection.cs</c> name (module-registration.md: "The
/// <c>DependencyInjection.cs</c> file serves as the module's composition root") per
/// CA1724: a type literally named <c>DependencyInjection</c> in a file that also
/// <c>using</c>s the <c>Microsoft.Extensions.DependencyInjection</c> namespace collides
/// with that namespace's own name. The file keeps the documented name; only the class
/// inside it is renamed to avoid the conflict.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHrisApplicationBehaviors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}

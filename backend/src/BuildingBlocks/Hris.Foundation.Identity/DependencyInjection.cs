using System.Reflection;
using FluentValidation;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Identity.Infrastructure.Persistence;
using Hris.Foundation.Identity.Infrastructure.Security;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Identity;

/// <summary>
/// Identity Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape
/// <c>Hris.Foundation.Configuration.ServiceCollectionExtensions</c> establishes,
/// unlike Logging Framework's own registration (see that class's own remarks): Identity
/// Framework defines <c>IEntityTypeConfiguration&lt;UserAccount&gt;</c> and a real
/// MediatR command/query surface of its own, so both <see cref="PersistenceAssemblyRegistry.Register"/>
/// and <c>AddMediatR</c> are called here, the same as Configuration Framework.
///
/// Called from <c>Hris.Api</c>'s <c>Program.cs</c> during the <c>AddFoundation()</c>
/// step, after <c>AddConfigurationFramework()</c> -- <c>AuthenticateCommandHandler</c>
/// issues a MediatR query against Configuration Framework (see that handler's own
/// remarks), the identical ordering reason <c>AddLoggingFramework()</c> already
/// documents in <c>Program.cs</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();

        // Singleton, not Scoped: both adapters are stateless -- PasswordHasher<TUser>'s
        // own PBKDF2 implementation and RandomNumberGenerator's static methods carry no
        // per-request state, the same reasoning AddLoggingFramework documents for its
        // own ILogSink/ILoggingService registrations.
        services.AddSingleton<IPasswordHasher, AspNetIdentityPasswordHasher>();
        services.AddSingleton<IMfaSecretProvisioner, OpaqueMfaSecretProvisioner>();

        return services;
    }
}

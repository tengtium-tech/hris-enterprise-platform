using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Hris.Modules.Leave.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Leave;

/// <summary>
/// Leave module's single registration entry point, per module-registration.md's Module
/// Entry Point section, the identical shape every prior module establishes. No
/// compile-time dependency on Timekeeping or Attendance (CTR-ARC-002): calendar facts
/// and absence-detection state arrive as already-resolved fields on this module's own
/// commands rather than being looked up from inside this module.
///
/// Registrations grow alongside each aggregate as it is built, the same incremental
/// sequence Attendance's own <c>AddAttendanceModule</c> followed. <see cref="LeaveType"/>
/// is the first (lowest-dependency) aggregate built this Sprint.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLeaveModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();

        return services;
    }
}

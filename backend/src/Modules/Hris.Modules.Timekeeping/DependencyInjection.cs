using System.Reflection;
using FluentValidation;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Hris.Modules.Timekeeping.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Timekeeping;

/// <summary>
/// Timekeeping module's single registration entry point, per
/// module-registration.md's Module Entry Point section, the identical shape every
/// prior module establishes. No compile-time dependency on any sibling module or
/// Phase 1 framework — including the Scheduling Framework, which this module
/// deliberately does not consume despite the shared word.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTimekeepingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
        services.AddScoped<IWorkShiftRepository, WorkShiftRepository>();
        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();
        services.AddScoped<IHolidayCalendarRepository, HolidayCalendarRepository>();

        return services;
    }
}

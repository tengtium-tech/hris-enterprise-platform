using System.Reflection;
using FluentValidation;
using Hris.Foundation.Events.Domain;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.Modules.Attendance.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Modules.Attendance;

/// <summary>
/// Attendance module's single registration entry point, per
/// module-registration.md's Module Entry Point section, the identical shape every
/// prior module establishes. No compile-time dependency on any sibling module,
/// including Timekeeping (CTR-ARC-002): its shift and holiday determination arrives
/// as already-resolved fields on <c>RunCalculationCommand</c> rather than being
/// looked up from inside this module.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IAttendanceAdjustmentRepository, AttendanceAdjustmentRepository>();
        services.AddScoped<IOvertimeRequestRepository, OvertimeRequestRepository>();
        services.AddScoped<IAttendancePolicyRepository, AttendancePolicyRepository>();
        services.AddScoped<IAttendanceDeviceRepository, AttendanceDeviceRepository>();
        services.AddScoped<IBiometricEnrollmentRepository, BiometricEnrollmentRepository>();

        // Two-phase adjustment subscribers (application/command-handlers.md). These implement the
        // platform's IDomainEventSubscriber<TEvent> contract, not MediatR, so they are registered
        // explicitly here. The outbox dispatcher resolves and invokes them in a fresh scope per poll;
        // until that dispatcher wires subscribers in, registration is inert and harmless.
        services.AddScoped<IDomainEventSubscriber<AttendanceAdjustmentSubmitted>, AttendanceAdjustmentSubmittedSubscriber>();
        services.AddScoped<IDomainEventSubscriber<AttendanceAdjustmentApproved>, AttendanceAdjustmentApprovedSubscriber>();
        services.AddScoped<IDomainEventSubscriber<AttendanceAdjustmentApplied>, AttendanceAdjustmentAppliedSubscriber>();
        services.AddScoped<IDomainEventSubscriber<AttendanceAdjustmentRejected>, AttendanceAdjustmentRejectedSubscriber>();
        services.AddScoped<IDomainEventSubscriber<AttendanceAdjustmentCancelled>, AttendanceAdjustmentCancelledSubscriber>();

        return services;
    }
}

using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Application.Commands;

/// <summary>
/// Registers a new tenant, per tenant-framework.md's own <c>RegisterTenantCommand</c>
/// row (Platform-Operator-Facing Commands and Queries). Carries every field that
/// document's own Carries column lists, including <c>DefaultLocale</c>/
/// <c>DefaultCurrency</c>/<c>TimeZone</c> and the initial Tenant Administrator's
/// name/email -- matching the document's own shape for forward compatibility, even
/// though this Sprint's own handler cannot act on them yet.
///
/// <see cref="RegisteredBy"/> is <c>null</c> for the self-service Starter-edition
/// path and a real <see cref="PlatformOperatorId"/> for the vendor-assisted path --
/// "both provisioning paths... resolve to the same command... the difference is who
/// submits the command, never what the command does once submitted" (that section's
/// own words).
///
/// Not authorization-gated the way a tenant-scoped write command would be: this
/// command is reachable only through a Platform Operator's own platform context (for
/// the vendor-assisted path) or an unauthenticated registration flow (self-service),
/// never a tenant context -- ADR-0009's own boundary is a routing/authentication
/// concern for whichever pipeline entry point eventually fronts this command (Sprint
/// 7's own API Platform, and a Platform Operator identity system no framework yet
/// builds), not a per-request <c>CheckAuthorizationQuery</c> check the way
/// Authorization Framework gates a tenant-scoped command -- that framework's own
/// <c>OrganizationalScopeLevel</c> has no Global level to check a platform-wide actor
/// against in the first place (localization-framework.md's own identical reasoning
/// for why its write commands are ungated).
/// </summary>
public sealed record RegisterTenantCommand(
    string TenantCode,
    string Organization,
    SubscriptionPlan SubscriptionPlan,
    string DefaultLocale,
    string DefaultCurrency,
    string TimeZone,
    string InitialAdministratorName,
    string InitialAdministratorEmail,
    PlatformOperatorId? RegisteredBy) : ICommand<Result<Guid>>;

internal sealed class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, Result<Guid>>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterTenantCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    /// <summary>
    /// Registers the <see cref="TenantAggregate"/> itself and stops -- Database
    /// Initialization, Configuration Creation, Branding Initialization, Process Pack
    /// Activation, and administrator invitation dispatch (this command's own Triggers
    /// column) are automated provisioning steps this Sprint cannot perform:
    /// `administration` module (TenantConfiguration), the Entitlement &amp; Process
    /// Pack Framework (Sprint 6), and Notification Framework (Sprint 5) do not exist
    /// in code yet. <see cref="DefaultLocale"/>/<see cref="DefaultCurrency"/>/
    /// <see cref="TimeZone"/>/<see cref="InitialAdministratorName"/>/
    /// <see cref="InitialAdministratorEmail"/> are accepted and validated for shape,
    /// then deliberately dropped rather than persisted anywhere -- there is no
    /// TenantConfiguration row yet to seed them into (Tenant Aggregate, Does Not Own),
    /// and inventing a placeholder location for them would misrecord data a future
    /// pass would then have to migrate off of. <c>CompleteTenantProvisioningCommand</c>
    /// is the narrow trigger this Sprint does own for the next step once its own
    /// precondition (a real TenantConfigurationId) exists.
    /// </summary>
    public async Task<Result<Guid>> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var tenantCodeResult = TenantCode.Create(request.TenantCode);
        if (tenantCodeResult.IsFailure)
        {
            return Result.Failure<Guid>(tenantCodeResult.Error);
        }

        if (await _repository.ExistsByTenantCodeAsync(tenantCodeResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(TenantErrors.TenantCodeAlreadyRegistered);
        }

        var registrationResult = TenantAggregate.Register(
            tenantCodeResult.Value,
            request.Organization,
            request.SubscriptionPlan,
            request.RegisteredBy,
            _timeProvider.GetUtcNow());

        if (registrationResult.IsFailure)
        {
            return Result.Failure<Guid>(registrationResult.Error);
        }

        await _repository.AddAsync(registrationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registrationResult.Value.Id.Value);
    }
}

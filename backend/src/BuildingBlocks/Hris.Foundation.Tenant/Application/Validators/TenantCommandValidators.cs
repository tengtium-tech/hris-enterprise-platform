using FluentValidation;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Application.Queries;

namespace Hris.Foundation.Tenant.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/mutation methods already enforce (tenant code format,
/// lifecycle-state gating, reason/compliance-basis non-empty) -- the identical
/// separation <c>LocalizationCommandValidators</c> states for its own set. Grouped
/// into one file for the same reason: most of these are the same one- or two-line
/// "field is not empty/not default" shape.
/// </summary>
public sealed class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(c => c.TenantCode).NotEmpty();
        RuleFor(c => c.Organization).NotEmpty();
        RuleFor(c => c.DefaultLocale).NotEmpty();
        RuleFor(c => c.DefaultCurrency).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
        RuleFor(c => c.InitialAdministratorName).NotEmpty();
        RuleFor(c => c.InitialAdministratorEmail).NotEmpty();
    }
}

public sealed class CompleteTenantProvisioningCommandValidator : AbstractValidator<CompleteTenantProvisioningCommand>
{
    public CompleteTenantProvisioningCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.TenantConfigurationId).NotEmpty();
    }
}

public sealed class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ActivatedBy).NotEmpty();
    }
}

public sealed class SuspendTenantCommandValidator : AbstractValidator<SuspendTenantCommand>
{
    public SuspendTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
        RuleFor(c => c.SuspendedBy).NotEmpty();
    }
}

public sealed class ReactivateTenantCommandValidator : AbstractValidator<ReactivateTenantCommand>
{
    public ReactivateTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ReactivatedBy).NotEmpty();
    }
}

public sealed class ArchiveTenantCommandValidator : AbstractValidator<ArchiveTenantCommand>
{
    public ArchiveTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
        RuleFor(c => c.ArchivedBy).NotEmpty();
    }
}

public sealed class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
        RuleFor(c => c.ComplianceBasis).NotEmpty();
        RuleFor(c => c.DeletedBy).NotEmpty();
    }
}

public sealed class ChangeTenantSubscriptionPlanCommandValidator : AbstractValidator<ChangeTenantSubscriptionPlanCommand>
{
    public ChangeTenantSubscriptionPlanCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ChangedBy).NotEmpty();
    }
}

public sealed class UpdateTenantOrganizationNameCommandValidator : AbstractValidator<UpdateTenantOrganizationNameCommand>
{
    public UpdateTenantOrganizationNameCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewOrganization).NotEmpty();
        RuleFor(c => c.UpdatedBy).NotEmpty();
    }
}

public sealed class GetTenantQueryValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

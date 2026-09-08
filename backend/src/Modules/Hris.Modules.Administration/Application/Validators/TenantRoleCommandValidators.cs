using FluentValidation;
using Hris.Modules.Administration.Application.Commands;

namespace Hris.Modules.Administration.Application.Validators;

public sealed class DefineTenantRoleCommandValidator : AbstractValidator<DefineTenantRoleCommand>
{
    public DefineTenantRoleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.CreatedBy).NotEmpty();
    }
}

public sealed class AddPermissionToTenantRoleCommandValidator : AbstractValidator<AddPermissionToTenantRoleCommand>
{
    public AddPermissionToTenantRoleCommandValidator()
    {
        RuleFor(c => c.TenantRoleId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Permission).NotEmpty();
        RuleFor(c => c.AddedBy).NotEmpty();
    }
}

public sealed class RemovePermissionFromTenantRoleCommandValidator : AbstractValidator<RemovePermissionFromTenantRoleCommand>
{
    public RemovePermissionFromTenantRoleCommandValidator()
    {
        RuleFor(c => c.TenantRoleId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PermissionGrantId).NotEmpty();
    }
}

public sealed class PublishTenantRoleCommandValidator : AbstractValidator<PublishTenantRoleCommand>
{
    public PublishTenantRoleCommandValidator()
    {
        RuleFor(c => c.TenantRoleId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PublishedBy).NotEmpty();
    }
}

public sealed class ChangeTenantRolePermissionsCommandValidator : AbstractValidator<ChangeTenantRolePermissionsCommand>
{
    public ChangeTenantRolePermissionsCommandValidator()
    {
        RuleFor(c => c.TenantRoleId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ChangedBy).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class DeprecateTenantRoleCommandValidator : AbstractValidator<DeprecateTenantRoleCommand>
{
    public DeprecateTenantRoleCommandValidator()
    {
        RuleFor(c => c.TenantRoleId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeleteTenantRoleCommandValidator : AbstractValidator<DeleteTenantRoleCommand>
{
    public DeleteTenantRoleCommandValidator()
    {
        RuleFor(c => c.TenantRoleId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

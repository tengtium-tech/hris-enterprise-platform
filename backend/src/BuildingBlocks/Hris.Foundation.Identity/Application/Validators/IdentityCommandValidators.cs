using FluentValidation;
using Hris.Foundation.Identity.Application.Commands;

namespace Hris.Foundation.Identity.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields, Data
/// formats, Input consistency, Business-independent validation." Deliberately does not
/// re-check anything <see cref="Domain.UserAccount"/>'s own factory/transition methods
/// already enforce (username length, lifecycle-transition legality, session/MFA
/// existence) -- the identical separation <c>ConfigurationCommandValidators</c> states
/// for its own six.
///
/// Grouped into one file for the same reason that file's six are: most of these
/// eleven validators are the same one- or two-line "id/string is not empty" shape.
/// </summary>
public sealed class CreateUserAccountCommandValidator : AbstractValidator<CreateUserAccountCommand>
{
    public CreateUserAccountCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Username).NotEmpty();
        RuleFor(c => c.EmailAddress).NotEmpty();
    }
}

public sealed class ActivateUserAccountCommandValidator : AbstractValidator<ActivateUserAccountCommand>
{
    public ActivateUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class LockUserAccountCommandValidator : AbstractValidator<LockUserAccountCommand>
{
    public LockUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class UnlockUserAccountCommandValidator : AbstractValidator<UnlockUserAccountCommand>
{
    public UnlockUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SuspendUserAccountCommandValidator : AbstractValidator<SuspendUserAccountCommand>
{
    public SuspendUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ReinstateUserAccountCommandValidator : AbstractValidator<ReinstateUserAccountCommand>
{
    public ReinstateUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DisableUserAccountCommandValidator : AbstractValidator<DisableUserAccountCommand>
{
    public DisableUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchiveUserAccountCommandValidator : AbstractValidator<ArchiveUserAccountCommand>
{
    public ArchiveUserAccountCommandValidator()
    {
        RuleFor(c => c.UserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class AuthenticateCommandValidator : AbstractValidator<AuthenticateCommand>
{
    public AuthenticateCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Username).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
        RuleFor(c => c.DeviceLabel).NotEmpty();
    }
}

public sealed class ChangeOwnPasswordCommandValidator : AbstractValidator<ChangeOwnPasswordCommand>
{
    public ChangeOwnPasswordCommandValidator()
    {
        RuleFor(c => c.ActorUserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CurrentPassword).NotEmpty();
        RuleFor(c => c.NewPassword).NotEmpty();
    }
}

public sealed class EnrollMfaFactorCommandValidator : AbstractValidator<EnrollMfaFactorCommand>
{
    public EnrollMfaFactorCommandValidator()
    {
        RuleFor(c => c.ActorUserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.FactorType).IsInEnum();
    }
}

public sealed class RemoveMfaFactorCommandValidator : AbstractValidator<RemoveMfaFactorCommand>
{
    public RemoveMfaFactorCommandValidator()
    {
        RuleFor(c => c.ActorUserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.MfaFactorId).NotEmpty();
    }
}

public sealed class RevokeMySessionCommandValidator : AbstractValidator<RevokeMySessionCommand>
{
    public RevokeMySessionCommandValidator()
    {
        RuleFor(c => c.ActorUserAccountId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SessionId).NotEmpty();
    }
}

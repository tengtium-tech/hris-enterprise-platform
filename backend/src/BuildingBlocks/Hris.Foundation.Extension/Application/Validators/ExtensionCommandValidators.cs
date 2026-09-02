using FluentValidation;
using Hris.Foundation.Extension.Application.Commands;
using Hris.Foundation.Extension.Application.Queries;

namespace Hris.Foundation.Extension.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (key format,
/// lifecycle-state gating, cross-aggregate hook-type support) -- the identical
/// separation every other framework's own validators file states for its own set.
/// </summary>
public sealed class RegisterExtensionPointCommandValidator : AbstractValidator<RegisterExtensionPointCommand>
{
    public RegisterExtensionPointCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.OwningModule).NotEmpty();
        RuleFor(c => c.SupportedHookTypes).NotNull();
    }
}

public sealed class PublishExtensionPointCommandValidator : AbstractValidator<PublishExtensionPointCommand>
{
    public PublishExtensionPointCommandValidator()
    {
        RuleFor(c => c.ExtensionPointId).NotEmpty();
    }
}

public sealed class DeprecateExtensionPointCommandValidator : AbstractValidator<DeprecateExtensionPointCommand>
{
    public DeprecateExtensionPointCommandValidator()
    {
        RuleFor(c => c.ExtensionPointId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class RetireExtensionPointCommandValidator : AbstractValidator<RetireExtensionPointCommand>
{
    public RetireExtensionPointCommandValidator()
    {
        RuleFor(c => c.ExtensionPointId).NotEmpty();
    }
}

public sealed class RegisterHookCommandValidator : AbstractValidator<RegisterHookCommand>
{
    public RegisterHookCommandValidator()
    {
        RuleFor(c => c.ExtensionPointId).NotEmpty();
        RuleFor(c => c.HandlerReference).NotEmpty();
        RuleFor(c => c.OwningModule).NotEmpty();
    }
}

public sealed class DisableHookCommandValidator : AbstractValidator<DisableHookCommand>
{
    public DisableHookCommandValidator()
    {
        RuleFor(c => c.HookId).NotEmpty();
    }
}

public sealed class EnableHookCommandValidator : AbstractValidator<EnableHookCommand>
{
    public EnableHookCommandValidator()
    {
        RuleFor(c => c.HookId).NotEmpty();
    }
}

public sealed class RemoveHookCommandValidator : AbstractValidator<RemoveHookCommand>
{
    public RemoveHookCommandValidator()
    {
        RuleFor(c => c.HookId).NotEmpty();
    }
}

public sealed class GetExtensionPointQueryValidator : AbstractValidator<GetExtensionPointQuery>
{
    public GetExtensionPointQueryValidator()
    {
        RuleFor(q => q.Key).NotEmpty();
    }
}

public sealed class ListHooksForExtensionPointQueryValidator : AbstractValidator<ListHooksForExtensionPointQuery>
{
    public ListHooksForExtensionPointQueryValidator()
    {
        RuleFor(q => q.ExtensionPointId).NotEmpty();
    }
}

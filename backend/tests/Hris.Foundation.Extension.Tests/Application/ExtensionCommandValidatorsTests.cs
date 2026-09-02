using FluentAssertions;
using Hris.Foundation.Extension.Application.Commands;
using Hris.Foundation.Extension.Application.Queries;
using Hris.Foundation.Extension.Application.Validators;
using Hris.Foundation.Extension.Domain;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>TenantCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class ExtensionCommandValidatorsTests
{
    [Fact]
    public void RegisterExtensionPointCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyKey()
    {
        var validator = new RegisterExtensionPointCommandValidator();
        var valid = new RegisterExtensionPointCommand(
            "employee.before-save", "Before Employee Save", "desc", ExtensionPointType.BusinessLogic, "Employee", [HookType.Before]);
        var invalid = valid with { Key = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishExtensionPointCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new PublishExtensionPointCommandValidator();

        validator.Validate(new PublishExtensionPointCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new PublishExtensionPointCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeprecateExtensionPointCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new DeprecateExtensionPointCommandValidator();
        var valid = new DeprecateExtensionPointCommand(Guid.NewGuid(), "Superseded.");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RetireExtensionPointCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RetireExtensionPointCommandValidator();

        validator.Validate(new RetireExtensionPointCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RetireExtensionPointCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterHookCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyHandlerReference()
    {
        var validator = new RegisterHookCommandValidator();
        var valid = new RegisterHookCommand(Guid.NewGuid(), HookType.Before, "Handler.Reference", "Employee");
        var invalid = valid with { HandlerReference = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DisableHookCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new DisableHookCommandValidator();

        validator.Validate(new DisableHookCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new DisableHookCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EnableHookCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new EnableHookCommandValidator();

        validator.Validate(new EnableHookCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new EnableHookCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveHookCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RemoveHookCommandValidator();

        validator.Validate(new RemoveHookCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RemoveHookCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetExtensionPointQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyKey()
    {
        var validator = new GetExtensionPointQueryValidator();

        validator.Validate(new GetExtensionPointQuery("employee.before-save")).IsValid.Should().BeTrue();
        validator.Validate(new GetExtensionPointQuery(string.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListHooksForExtensionPointQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new ListHooksForExtensionPointQueryValidator();

        validator.Validate(new ListHooksForExtensionPointQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ListHooksForExtensionPointQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }
}

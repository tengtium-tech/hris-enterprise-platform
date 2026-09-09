using FluentAssertions;
using Hris.Modules.Workflow.Application.Commands;
using Hris.Modules.Workflow.Application.Validators;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

public sealed class WorkflowCommandValidatorsTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actor = Guid.NewGuid();

    [Fact]
    public void AuthorValidator_Passes_ForAWellFormedCommand()
    {
        var validator = new AuthorWorkflowDefinitionCommandValidator();

        var result = validator.Validate(
            new AuthorWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), "Leave Approval", null, null, _actor));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AuthorValidator_Fails_WhenNameIsMissingOrTenantIsEmpty()
    {
        var validator = new AuthorWorkflowDefinitionCommandValidator();

        validator.Validate(new AuthorWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), "", null, null, _actor))
            .IsValid.Should().BeFalse();
        validator.Validate(new AuthorWorkflowDefinitionCommand(Guid.Empty, Guid.NewGuid(), "Name", null, null, _actor))
            .IsValid.Should().BeFalse();
        validator.Validate(new AuthorWorkflowDefinitionCommand(_tenantId, Guid.Empty, "Name", null, null, _actor))
            .IsValid.Should().BeFalse();
        validator.Validate(new AuthorWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), "Name", null, null, Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void AuthorValidator_Fails_WhenNameExceedsTheColumnLength()
    {
        var validator = new AuthorWorkflowDefinitionCommandValidator();

        validator.Validate(new AuthorWorkflowDefinitionCommand(
            _tenantId, Guid.NewGuid(), new string('x', 201), null, null, _actor)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EditDraftValidator_ChecksIdentifiersAndStepPresence()
    {
        var validator = new EditDraftDefinitionCommandValidator();

        validator.Validate(new EditDraftDefinitionCommand(_tenantId, Guid.NewGuid(), [], _actor)).IsValid.Should().BeTrue();
        validator.Validate(new EditDraftDefinitionCommand(Guid.Empty, Guid.NewGuid(), [], _actor)).IsValid.Should().BeFalse();
        validator.Validate(new EditDraftDefinitionCommand(_tenantId, Guid.Empty, [], _actor)).IsValid.Should().BeFalse();
        validator.Validate(new EditDraftDefinitionCommand(_tenantId, Guid.NewGuid(), null!, _actor)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishValidator_ChecksIdentifiers()
    {
        var validator = new PublishWorkflowDefinitionCommandValidator();

        validator.Validate(new PublishWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), false, false, false, _actor))
            .IsValid.Should().BeTrue();
        validator.Validate(new PublishWorkflowDefinitionCommand(_tenantId, Guid.Empty, false, false, false, _actor))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateNewVersionValidator_ChecksIdentifiers()
    {
        var validator = new CreateNewVersionCommandValidator();

        validator.Validate(new CreateNewVersionCommand(_tenantId, Guid.NewGuid(), _actor)).IsValid.Should().BeTrue();
        validator.Validate(new CreateNewVersionCommand(_tenantId, Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeprecateValidator_RequiresAReason()
    {
        var validator = new DeprecateWorkflowDefinitionCommandValidator();

        validator.Validate(new DeprecateWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), _actor, "Superseded"))
            .IsValid.Should().BeTrue();
        validator.Validate(new DeprecateWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), _actor, ""))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateApprovalPolicyValidator_ChecksIdentifiers()
    {
        var validator = new CreateApprovalPolicyCommandValidator();

        validator.Validate(new CreateApprovalPolicyCommand(_tenantId, _actor)).IsValid.Should().BeTrue();
        validator.Validate(new CreateApprovalPolicyCommand(Guid.Empty, _actor)).IsValid.Should().BeFalse();
    }

    /// <summary>
    /// The reason is checked at the shape level as well as in the aggregate, because
    /// a policy change that removes an approval requirement is a control removal and
    /// the caller should be told before the command is dispatched.
    /// </summary>
    [Fact]
    public void ConfigureApprovalPolicyValidator_RequiresAReason()
    {
        var validator = new ConfigureApprovalPolicyCommandValidator();

        validator.Validate(new ConfigureApprovalPolicyCommand(
            _tenantId, null, null, null, null, null, null, null, null, true, false, _actor, "Reason"))
            .IsValid.Should().BeTrue();

        validator.Validate(new ConfigureApprovalPolicyCommand(
            _tenantId, null, null, null, null, null, null, null, null, true, false, _actor, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateDelegationValidator_RequiresAnOrderedPeriodAndAReason()
    {
        var validator = new CreateApprovalDelegationCommandValidator();
        var today = TestWorkflow.Today;

        validator.Validate(new CreateApprovalDelegationCommand(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, true, today, today.AddDays(5), "Cover", null, false, _actor))
            .IsValid.Should().BeTrue();

        validator.Validate(new CreateApprovalDelegationCommand(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, true, today, today.AddDays(-1), "Cover", null, false, _actor))
            .IsValid.Should().BeFalse();

        validator.Validate(new CreateApprovalDelegationCommand(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), null, true, today, today.AddDays(5), " ", null, false, _actor))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void DelegationLifecycleValidators_CheckIdentifiers()
    {
        new ActivateApprovalDelegationCommandValidator()
            .Validate(new ActivateApprovalDelegationCommand(_tenantId, Guid.NewGuid(), false)).IsValid.Should().BeTrue();
        new ActivateApprovalDelegationCommandValidator()
            .Validate(new ActivateApprovalDelegationCommand(_tenantId, Guid.Empty, false)).IsValid.Should().BeFalse();

        new ExpireApprovalDelegationCommandValidator()
            .Validate(new ExpireApprovalDelegationCommand(_tenantId, Guid.NewGuid())).IsValid.Should().BeTrue();
        new ExpireApprovalDelegationCommandValidator()
            .Validate(new ExpireApprovalDelegationCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();

        new RevokeApprovalDelegationCommandValidator()
            .Validate(new RevokeApprovalDelegationCommand(_tenantId, Guid.NewGuid(), _actor, "Reason"))
            .IsValid.Should().BeTrue();
        new RevokeApprovalDelegationCommandValidator()
            .Validate(new RevokeApprovalDelegationCommand(_tenantId, Guid.NewGuid(), _actor, ""))
            .IsValid.Should().BeFalse();
    }
}

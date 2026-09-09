using FluentAssertions;
using Hris.Modules.Workflow.Application.Queries;
using Hris.Modules.Workflow.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

public sealed class WorkflowDefinitionQueryHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task GetById_ReturnsTheDefinition_WithinTheTenant()
    {
        var definition = TestWorkflow.Published(_tenantId);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new GetWorkflowDefinitionByIdQueryHandler(_repository);

        var result = await handler.Handle(
            new GetWorkflowDefinitionByIdQuery(definition.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(definition.Name);
        result.Value.Steps.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForAnotherTenantsDefinition()
    {
        var definition = TestWorkflow.Published(Guid.NewGuid());
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new GetWorkflowDefinitionByIdQueryHandler(_repository);

        var result = await handler.Handle(
            new GetWorkflowDefinitionByIdQuery(definition.Id.Value, _tenantId), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotFound);
    }

    [Fact]
    public async Task List_ReturnsEveryDefinitionInTheTenant_WhenUnfiltered()
    {
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns([TestWorkflow.Published(_tenantId), TestWorkflow.Draft(_tenantId)]);
        var handler = new ListWorkflowDefinitionsQueryHandler(_repository);

        var result = await handler.Handle(new ListWorkflowDefinitionsQuery(_tenantId, null, null), CancellationToken.None);

        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_FiltersByStatus_CaseInsensitively()
    {
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns([TestWorkflow.Published(_tenantId), TestWorkflow.Draft(_tenantId)]);
        var handler = new ListWorkflowDefinitionsQueryHandler(_repository);

        var result = await handler.Handle(new ListWorkflowDefinitionsQuery(_tenantId, null, "published"), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.Status.Should().Be("Published");
    }

    [Fact]
    public async Task List_FiltersByBusinessProcess()
    {
        var wanted = TestWorkflow.Published(_tenantId);
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns([wanted, TestWorkflow.Draft(_tenantId)]);
        var handler = new ListWorkflowDefinitionsQueryHandler(_repository);

        var result = await handler.Handle(
            new ListWorkflowDefinitionsQuery(_tenantId, wanted.BusinessProcessId, null), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.Id.Should().Be(wanted.Id.Value);
    }

    /// <summary>
    /// The version-history query must reach superseded versions, not only the
    /// current one, because workflow-versioning.md's guarantee is otherwise not
    /// checkable.
    /// </summary>
    [Fact]
    public async Task VersionHistory_ReturnsEveryVersionInTheLineage()
    {
        var first = TestWorkflow.Published(_tenantId);
        var second = first.CreateNewVersion(new WorkflowDefinitionId(Guid.NewGuid()), Guid.NewGuid(), TestWorkflow.NowUtc).Value;
        _repository.ListByLineageAsync(first.LineageId, Arg.Any<CancellationToken>()).Returns([first, second]);
        var handler = new GetDefinitionVersionHistoryQueryHandler(_repository);

        var result = await handler.Handle(
            new GetDefinitionVersionHistoryQuery(first.LineageId, _tenantId), CancellationToken.None);

        result.Value.Select(v => v.Version).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task VersionHistory_ExcludesVersionsFromAnotherTenant()
    {
        var mine = TestWorkflow.Published(_tenantId);
        var theirs = TestWorkflow.Published(Guid.NewGuid());
        _repository.ListByLineageAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([mine, theirs]);
        var handler = new GetDefinitionVersionHistoryQueryHandler(_repository);

        var result = await handler.Handle(
            new GetDefinitionVersionHistoryQuery(mine.LineageId, _tenantId), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.TenantId.Should().Be(_tenantId);
    }
}

public sealed class ApprovalPolicyQueryHandlerTests
{
    private readonly IApprovalPolicyRepository _repository = Substitute.For<IApprovalPolicyRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Get_ReturnsThePolicy()
    {
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(TestWorkflow.Policy(_tenantId));
        var handler = new GetApprovalPolicyQueryHandler(_repository);

        var result = await handler.Handle(new GetApprovalPolicyQuery(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenTheTenantHasNoPolicy()
    {
        _repository.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns((ApprovalPolicy?)null);
        var handler = new GetApprovalPolicyQueryHandler(_repository);

        var result = await handler.Handle(new GetApprovalPolicyQuery(_tenantId), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.ApprovalPolicyNotFound);
    }
}

public sealed class ApprovalDelegationQueryHandlerTests
{
    private readonly IApprovalDelegationRepository _repository = Substitute.For<IApprovalDelegationRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Get_ReturnsTheDelegation()
    {
        var delegation = TestWorkflow.Delegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);
        var handler = new GetApprovalDelegationQueryHandler(_repository);

        var result = await handler.Handle(
            new GetApprovalDelegationQuery(delegation.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Scheduled");
    }

    [Fact]
    public async Task Get_ReturnsNotFound_ForAnotherTenantsDelegation()
    {
        var delegation = TestWorkflow.Delegation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);
        var handler = new GetApprovalDelegationQueryHandler(_repository);

        var result = await handler.Handle(
            new GetApprovalDelegationQuery(delegation.Id.Value, _tenantId), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DelegationNotFound);
    }

    [Fact]
    public async Task List_ReadsByTenant_WhenNoDelegatorIsGiven()
    {
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns([TestWorkflow.Delegation(_tenantId, Guid.NewGuid(), Guid.NewGuid())]);
        var handler = new ListApprovalDelegationsQueryHandler(_repository);

        var result = await handler.Handle(new ListApprovalDelegationsQuery(_tenantId, null), CancellationToken.None);

        result.Value.Should().ContainSingle();
        await _repository.DidNotReceive().ListByDelegatorAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_ReadsByDelegator_WhenOneIsGiven()
    {
        var delegator = Guid.NewGuid();
        _repository.ListByDelegatorAsync(_tenantId, delegator, Arg.Any<CancellationToken>())
            .Returns([TestWorkflow.Delegation(_tenantId, delegator, Guid.NewGuid())]);
        var handler = new ListApprovalDelegationsQueryHandler(_repository);

        var result = await handler.Handle(new ListApprovalDelegationsQuery(_tenantId, delegator), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.DelegatorUserAccountId.Should().Be(delegator);
    }
}

using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Queries;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class WorkflowInstanceQueryHandlerTests
{
    private readonly IWorkflowInstanceRepository _repository = Substitute.For<IWorkflowInstanceRepository>();

    [Fact]
    public async Task GetWorkflowInstanceQuery_Succeeds_AndReturnsEveryFieldMapped()
    {
        var instance = TestData.InProgressInstance();
        instance.Fail("Downstream module command failed", TestData.NowUtc);
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new GetWorkflowInstanceQueryHandler(_repository);

        var result = await handler.Handle(new GetWorkflowInstanceQuery(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.WorkflowInstanceId.Should().Be(instance.Id.Value);
        dto.TenantId.Should().Be(instance.TenantId);
        dto.WorkflowDefinitionId.Should().Be(instance.WorkflowDefinitionId.Value);
        dto.WorkflowDefinitionVersionNumber.Should().Be(instance.WorkflowDefinitionVersionNumber);
        dto.TriggeringReference.Should().Be(instance.TriggeringReference);
        dto.InitiatedByUserId.Should().Be(instance.InitiatedByUserId);
        dto.Status.Should().Be(instance.Status.ToString());
        dto.CurrentStepOrder.Should().Be(instance.CurrentStepOrder);
        dto.StartedAtUtc.Should().Be(instance.StartedAtUtc);
        dto.CompletedAtUtc.Should().Be(instance.CompletedAtUtc);
        dto.FailureReason.Should().Be(instance.FailureReason);
    }

    [Fact]
    public async Task GetWorkflowInstanceQuery_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new GetWorkflowInstanceQueryHandler(_repository);

        var result = await handler.Handle(new GetWorkflowInstanceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task ListWorkflowInstanceHistoryQuery_Succeeds_AndReturnsMappedDtos()
    {
        var definitionId = new WorkflowDefinitionId(Guid.NewGuid());
        var instances = new List<WorkflowInstance> { TestData.SubmittedInstance(workflowDefinitionId: definitionId) };
        _repository.ListByDefinitionAsync(definitionId, TestData.TenantId, Arg.Any<CancellationToken>()).Returns(instances);
        var handler = new ListWorkflowInstanceHistoryQueryHandler(_repository);

        var result = await handler.Handle(new ListWorkflowInstanceHistoryQuery(definitionId.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].WorkflowInstanceId.Should().Be(instances[0].Id.Value);
    }
}

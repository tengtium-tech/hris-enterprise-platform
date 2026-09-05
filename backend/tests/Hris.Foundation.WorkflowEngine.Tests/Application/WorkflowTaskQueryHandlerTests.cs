using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Queries;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class WorkflowTaskQueryHandlerTests
{
    private readonly IWorkflowTaskRepository _repository = Substitute.For<IWorkflowTaskRepository>();

    [Fact]
    public async Task GetWorkflowTaskQuery_Succeeds_AndReturnsEveryFieldMapped()
    {
        var task = TestData.PendingTask();
        task.Delegate(Guid.NewGuid(), "Out of office", TestData.NowUtc);
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new GetWorkflowTaskQueryHandler(_repository);

        var result = await handler.Handle(new GetWorkflowTaskQuery(task.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.WorkflowTaskId.Should().Be(task.Id.Value);
        dto.TenantId.Should().Be(task.TenantId);
        dto.WorkflowInstanceId.Should().Be(task.WorkflowInstanceId.Value);
        dto.StepName.Should().Be(task.StepName);
        dto.StepOrder.Should().Be(task.StepOrder);
        dto.ParticipantType.Should().Be(task.ParticipantType.ToString());
        dto.ParticipantRoleName.Should().Be(task.ParticipantRoleName);
        dto.AssignedToUserId.Should().Be(task.AssignedToUserId);
        dto.Status.Should().Be(task.Status.ToString());
        dto.Comments.Should().Be(task.Comments);
        dto.DelegatedToUserId.Should().Be(task.DelegatedToUserId);
        dto.EscalationLevel.Should().Be(task.EscalationLevel);
        dto.CreatedAtUtc.Should().Be(task.CreatedAtUtc);
        dto.CompletedAtUtc.Should().Be(task.CompletedAtUtc);
    }

    [Fact]
    public async Task GetWorkflowTaskQuery_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new GetWorkflowTaskQueryHandler(_repository);

        var result = await handler.Handle(new GetWorkflowTaskQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }

    [Fact]
    public async Task ListMyWorkflowTasksQuery_Succeeds_AndReturnsMappedDtos()
    {
        var assigneeId = Guid.NewGuid();
        var tasks = new List<WorkflowTask> { TestData.PendingTask(assignedToUserId: assigneeId) };
        _repository.ListByAssigneeAsync(assigneeId, TestData.TenantId, Arg.Any<CancellationToken>()).Returns(tasks);
        var handler = new ListMyWorkflowTasksQueryHandler(_repository);

        var result = await handler.Handle(new ListMyWorkflowTasksQuery(assigneeId, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].WorkflowTaskId.Should().Be(tasks[0].Id.Value);
    }
}

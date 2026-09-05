using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Application.Queries;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class WorkflowDefinitionQueryHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();

    [Fact]
    public async Task GetWorkflowDefinitionQuery_Succeeds_AndReturnsEveryFieldMapped()
    {
        var definition = TestData.PublishedDefinition();
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new GetWorkflowDefinitionQueryHandler(_repository);

        var result = await handler.Handle(new GetWorkflowDefinitionQuery(definition.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.WorkflowDefinitionId.Should().Be(definition.Id.Value);
        dto.TenantId.Should().Be(definition.TenantId);
        dto.Name.Should().Be(definition.Name);
        dto.TriggerType.Should().Be(definition.TriggerType.ToString());
        dto.TriggerExpression.Should().Be(definition.TriggerExpression);
        dto.CreatedAtUtc.Should().Be(definition.CreatedAtUtc);
        dto.Versions.Should().ContainSingle();

        var versionDto = dto.Versions[0];
        var version = definition.Versions[0];
        versionDto.WorkflowDefinitionVersionId.Should().Be(version.Id.Value);
        versionDto.VersionNumber.Should().Be(version.VersionNumber);
        versionDto.Status.Should().Be(version.Status.ToString());
        versionDto.CreatedAtUtc.Should().Be(version.CreatedAtUtc);
        versionDto.PublishedAtUtc.Should().Be(version.PublishedAtUtc);
        versionDto.Steps.Should().HaveSameCount(version.Steps);

        var stepDto = versionDto.Steps[0];
        var step = version.Steps[0];
        stepDto.StepName.Should().Be(step.StepName);
        stepDto.StepType.Should().Be(step.StepType.ToString());
        stepDto.Order.Should().Be(step.Order);
        stepDto.ParticipantType.Should().Be(step.ParticipantType?.ToString());
        stepDto.ParticipantRoleName.Should().Be(step.ParticipantRoleName);
        stepDto.ActionName.Should().Be(step.ActionName);
        stepDto.NotificationTemplateKey.Should().Be(step.NotificationTemplateKey);
    }

    [Fact]
    public async Task GetWorkflowDefinitionQuery_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        var handler = new GetWorkflowDefinitionQueryHandler(_repository);

        var result = await handler.Handle(new GetWorkflowDefinitionQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNotFound);
    }

    [Fact]
    public async Task ListWorkflowDefinitionsQuery_Succeeds_AndReturnsMappedDtos()
    {
        var definitions = new List<WorkflowDefinition> { TestData.NewDefinition() };
        _repository.ListByTenantAsync(TestData.TenantId, Arg.Any<CancellationToken>()).Returns(definitions);
        var handler = new ListWorkflowDefinitionsQueryHandler(_repository);

        var result = await handler.Handle(new ListWorkflowDefinitionsQuery(TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].WorkflowDefinitionId.Should().Be(definitions[0].Id.Value);
    }
}

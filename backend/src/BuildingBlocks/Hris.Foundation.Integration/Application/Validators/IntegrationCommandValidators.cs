using FluentValidation;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Application.Queries;

namespace Hris.Foundation.Integration.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (name/category/
/// endpoint shape, lifecycle-state gating) -- the identical separation every other
/// framework's own validators file states for its own set.
/// </summary>
public sealed class RegisterConnectorCommandValidator : AbstractValidator<RegisterConnectorCommand>
{
    public RegisterConnectorCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.IntegrationCategory).NotEmpty();
        RuleFor(c => c.EndpointType).NotEmpty();
        RuleFor(c => c.EndpointAddress).NotEmpty();
    }
}

public sealed class ActivateConnectorCommandValidator : AbstractValidator<ActivateConnectorCommand>
{
    public ActivateConnectorCommandValidator()
    {
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SuspendConnectorCommandValidator : AbstractValidator<SuspendConnectorCommand>
{
    public SuspendConnectorCommandValidator()
    {
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeactivateConnectorCommandValidator : AbstractValidator<DeactivateConnectorCommand>
{
    public DeactivateConnectorCommandValidator()
    {
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class UpdateConnectorEndpointCommandValidator : AbstractValidator<UpdateConnectorEndpointCommand>
{
    public UpdateConnectorEndpointCommandValidator()
    {
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EndpointType).NotEmpty();
        RuleFor(c => c.EndpointAddress).NotEmpty();
    }
}

public sealed class AddDataMappingCommandValidator : AbstractValidator<AddDataMappingCommand>
{
    public AddDataMappingCommandValidator()
    {
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SourceField).NotEmpty();
        RuleFor(c => c.TargetField).NotEmpty();
        RuleFor(c => c.TransformationType).NotEmpty();
    }
}

public sealed class RemoveDataMappingCommandValidator : AbstractValidator<RemoveDataMappingCommand>
{
    public RemoveDataMappingCommandValidator()
    {
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DataMappingId).NotEmpty();
    }
}

public sealed class StartIntegrationRunCommandValidator : AbstractValidator<StartIntegrationRunCommand>
{
    public StartIntegrationRunCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ConnectorId).NotEmpty();
        RuleFor(c => c.RunKind).NotEmpty();
        RuleFor(c => c.MaxRetries).GreaterThanOrEqualTo(0);
    }
}

public sealed class CompleteIntegrationRunCommandValidator : AbstractValidator<CompleteIntegrationRunCommand>
{
    public CompleteIntegrationRunCommandValidator()
    {
        RuleFor(c => c.IntegrationRunId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.RecordsProcessed).GreaterThanOrEqualTo(0);
    }
}

public sealed class FailIntegrationRunCommandValidator : AbstractValidator<FailIntegrationRunCommand>
{
    public FailIntegrationRunCommandValidator()
    {
        RuleFor(c => c.IntegrationRunId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class RetryIntegrationRunCommandValidator : AbstractValidator<RetryIntegrationRunCommand>
{
    public RetryIntegrationRunCommandValidator()
    {
        RuleFor(c => c.IntegrationRunId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class MoveIntegrationRunToDeadLetterCommandValidator : AbstractValidator<MoveIntegrationRunToDeadLetterCommand>
{
    public MoveIntegrationRunToDeadLetterCommandValidator()
    {
        RuleFor(c => c.IntegrationRunId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class GetConnectorQueryValidator : AbstractValidator<GetConnectorQuery>
{
    public GetConnectorQueryValidator()
    {
        RuleFor(q => q.ConnectorId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class ListConnectorsQueryValidator : AbstractValidator<ListConnectorsQuery>
{
    public ListConnectorsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetIntegrationRunQueryValidator : AbstractValidator<GetIntegrationRunQuery>
{
    public GetIntegrationRunQueryValidator()
    {
        RuleFor(q => q.IntegrationRunId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class ListIntegrationRunHistoryQueryValidator : AbstractValidator<ListIntegrationRunHistoryQuery>
{
    public ListIntegrationRunHistoryQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ConnectorId).NotEmpty();
    }
}

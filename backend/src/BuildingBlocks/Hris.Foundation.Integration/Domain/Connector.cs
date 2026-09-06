using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Aggregate Root of the Integration Framework's own reusable integration
/// registration. Source: docs/03-foundation/integration-framework.md, Core Concepts
/// ("A Connector encapsulates integration logic... Connectors should be reusable and
/// independently configurable").
///
/// Deliberately collapses this document's own "Integration" (the business
/// relationship, e.g. "ERP") and "Connector" (the concrete implementation, e.g. "SAP
/// Connector") concepts into a single Aggregate Root -- <see cref="IntegrationCategory"/>
/// carries the former, <see cref="Name"/> the latter. Splitting them into two
/// Aggregates would only be justified by a real one-to-many relationship this
/// document's own examples never actually show (nothing in Typical Integrations
/// suggests a tenant registers, say, both a "SAP Connector" and an "Oracle Connector"
/// under one shared "ERP Integration" record simultaneously); collapsing avoids
/// inventing that relationship ahead of a real need.
///
/// <see cref="TenantId"/> is a plain, caller-supplied <see cref="Guid"/>, the same
/// "built concretely" choice this codebase's own tenant-scoped aggregates already
/// make for themselves -- this document's own AI Implementation Guidance states
/// <c>CTR-ISO-004</c> explicitly ("Carry tenant context through every inbound and
/// outbound integration").
///
/// <see cref="IntegrationCategory"/> is a validated, non-empty string rather than a
/// closed enum -- this document's own Typical Integrations examples (Identity,
/// Finance, Government, Communication, Attendance, Documents) are illustrative, not
/// exhaustive, the same "business modules invent their own values" reasoning already
/// governing <c>CacheKey.Region</c> and <c>DocumentAttachment.EntityType</c> in this
/// codebase. <see cref="EndpointType"/>, by contrast, is a closed enum -- see that
/// type's own remarks for why.
/// </summary>
public sealed class Connector : AggregateRoot<ConnectorId>
{
    private readonly List<DataMapping> _mappings = [];

    public Guid TenantId { get; }

    public string Name { get; private set; }

    public string IntegrationCategory { get; }

    public EndpointType EndpointType { get; private set; }

    public string EndpointAddress { get; private set; }

    public ConnectorStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<DataMapping> Mappings => _mappings.AsReadOnly();

    private Connector(
        ConnectorId id,
        Guid tenantId,
        string name,
        string integrationCategory,
        EndpointType endpointType,
        string endpointAddress,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        IntegrationCategory = integrationCategory;
        EndpointType = endpointType;
        EndpointAddress = endpointAddress;
        Status = ConnectorStatus.Registered;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Registers a new connector, in <see cref="ConnectorStatus.Registered"/>. Raises
    /// <see cref="ConnectorRegistered"/>.
    /// </summary>
    public static Result<Connector> Register(
        Guid tenantId,
        string? name,
        string? integrationCategory,
        EndpointType endpointType,
        string? endpointAddress,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Connector>(IntegrationErrors.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(integrationCategory))
        {
            return Result.Failure<Connector>(IntegrationErrors.IntegrationCategoryRequired);
        }

        if (string.IsNullOrWhiteSpace(endpointAddress))
        {
            return Result.Failure<Connector>(IntegrationErrors.EndpointAddressRequired);
        }

        var connector = new Connector(
            new ConnectorId(Guid.NewGuid()), tenantId, name.Trim(), integrationCategory.Trim(), endpointType, endpointAddress.Trim(),
            nowUtc);

        connector.AddDomainEvent(new ConnectorRegistered(Guid.NewGuid(), nowUtc, connector.Id, tenantId, connector.IntegrationCategory));

        return Result.Success(connector);
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (Status is not (ConnectorStatus.Registered or ConnectorStatus.Suspended))
        {
            return Result.Failure(IntegrationErrors.InvalidConnectorLifecycleTransition);
        }

        Status = ConnectorStatus.Active;
        AddDomainEvent(new ConnectorUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Suspend(DateTimeOffset nowUtc)
    {
        if (Status != ConnectorStatus.Active)
        {
            return Result.Failure(IntegrationErrors.InvalidConnectorLifecycleTransition);
        }

        Status = ConnectorStatus.Suspended;
        AddDomainEvent(new ConnectorUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Terminal from any other status -- an administrator must always be able to
    /// retire a connector regardless of how far its own lifecycle progressed, the
    /// same broader-than-strictly-sequential guard <c>Job.Cancel</c>'s own remarks
    /// justify for itself.
    /// </summary>
    public Result Deactivate(DateTimeOffset nowUtc)
    {
        if (Status == ConnectorStatus.Deactivated)
        {
            return Result.Failure(IntegrationErrors.InvalidConnectorLifecycleTransition);
        }

        Status = ConnectorStatus.Deactivated;
        AddDomainEvent(new ConnectorUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Rejected once <see cref="Status"/> reaches <see cref="ConnectorStatus.Deactivated"/>
    /// -- a deactivated connector's own configuration is historical record from that
    /// point on, the same terminal-state-rejects-edits reasoning
    /// <c>Document.UpdateMetadata</c> already applies once a document is Disposed.
    /// </summary>
    public Result UpdateEndpoint(EndpointType endpointType, string? endpointAddress, DateTimeOffset nowUtc)
    {
        if (Status == ConnectorStatus.Deactivated)
        {
            return Result.Failure(IntegrationErrors.InvalidConnectorLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(endpointAddress))
        {
            return Result.Failure(IntegrationErrors.EndpointAddressRequired);
        }

        EndpointType = endpointType;
        EndpointAddress = endpointAddress.Trim();
        AddDomainEvent(new ConnectorUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result<Guid> AddDataMapping(
        string? sourceField, string? targetField, TransformationType transformationType, string? transformationRule,
        DateTimeOffset nowUtc)
    {
        if (Status == ConnectorStatus.Deactivated)
        {
            return Result.Failure<Guid>(IntegrationErrors.InvalidConnectorLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(sourceField))
        {
            return Result.Failure<Guid>(IntegrationErrors.SourceFieldRequired);
        }

        if (string.IsNullOrWhiteSpace(targetField))
        {
            return Result.Failure<Guid>(IntegrationErrors.TargetFieldRequired);
        }

        var mapping = new DataMapping(
            new DataMappingId(Guid.NewGuid()), sourceField.Trim(), targetField.Trim(), transformationType,
            string.IsNullOrWhiteSpace(transformationRule) ? null : transformationRule.Trim());

        _mappings.Add(mapping);
        AddDomainEvent(new ConnectorUpdated(Guid.NewGuid(), nowUtc, Id));

        return Result.Success(mapping.Id.Value);
    }

    public Result RemoveDataMapping(Guid dataMappingId, DateTimeOffset nowUtc)
    {
        var mapping = _mappings.FirstOrDefault(m => m.Id.Value == dataMappingId);
        if (mapping is null)
        {
            return Result.Failure(IntegrationErrors.DataMappingNotFound);
        }

        _mappings.Remove(mapping);
        AddDomainEvent(new ConnectorUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}

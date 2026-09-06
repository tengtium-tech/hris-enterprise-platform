using Hris.Foundation.Integration.Domain;

namespace Hris.Foundation.Integration.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.NewGuid();

    /// <summary>A newly registered connector, in <see cref="ConnectorStatus.Registered"/>.</summary>
    public static Connector RegisteredConnector(
        Guid? tenantId = null,
        string name = "SAP Connector",
        string integrationCategory = "ERP",
        EndpointType endpointType = EndpointType.RestApi,
        string endpointAddress = "https://sap.example.com/api",
        DateTimeOffset? nowUtc = null) =>
        Connector.Register(tenantId ?? TenantId, name, integrationCategory, endpointType, endpointAddress, nowUtc ?? NowUtc).Value;

    /// <summary>A connector walked through to <see cref="ConnectorStatus.Active"/>.</summary>
    public static Connector ActiveConnector(DateTimeOffset? nowUtc = null)
    {
        var connector = RegisteredConnector(nowUtc: nowUtc);
        connector.Activate(nowUtc ?? NowUtc);
        return connector;
    }

    /// <summary>A connector walked through to <see cref="ConnectorStatus.Suspended"/>.</summary>
    public static Connector SuspendedConnector(DateTimeOffset? nowUtc = null)
    {
        var connector = ActiveConnector(nowUtc);
        connector.Suspend(nowUtc ?? NowUtc);
        return connector;
    }

    /// <summary>A connector walked through to <see cref="ConnectorStatus.Deactivated"/>.</summary>
    public static Connector DeactivatedConnector(DateTimeOffset? nowUtc = null)
    {
        var connector = ActiveConnector(nowUtc);
        connector.Deactivate(nowUtc ?? NowUtc);
        return connector;
    }

    /// <summary>A newly started run, in <see cref="IntegrationRunStatus.Started"/>.</summary>
    public static IntegrationRun StartedRun(
        Guid? tenantId = null,
        ConnectorId? connectorId = null,
        IntegrationRunKind runKind = IntegrationRunKind.IntegrationCall,
        int maxRetries = 3,
        DateTimeOffset? nowUtc = null) =>
        IntegrationRun.Start(
            tenantId ?? TenantId, connectorId ?? new ConnectorId(Guid.NewGuid()), runKind, syncModel: null, jobId: null, maxRetries,
            nowUtc ?? NowUtc).Value;

    /// <summary>A run walked through to <see cref="IntegrationRunStatus.Failed"/>.</summary>
    public static IntegrationRun FailedRun(DateTimeOffset? nowUtc = null)
    {
        var run = StartedRun(nowUtc: nowUtc);
        run.Fail("transient failure", nowUtc ?? NowUtc);
        return run;
    }
}

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Source: docs/03-foundation/integration-framework.md, Core Concepts ("Connectors
/// should be reusable and independently configurable"). The document names no
/// explicit lifecycle diagram for a Connector the way it does for Retry Management --
/// this is the smallest state machine that satisfies "independently configurable"
/// (a connector can be turned off without being deleted) while still distinguishing a
/// freshly registered, not-yet-verified connector from one actively in use.
/// </summary>
public enum ConnectorStatus
{
    Registered = 0,
    Active,
    Suspended,
    Deactivated,
}

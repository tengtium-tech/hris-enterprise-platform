namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Source: docs/03-foundation/integration-framework.md, Core Concepts ("Endpoints
/// represent communication interfaces... REST API, SOAP Service, GraphQL API,
/// Webhook, SFTP, Message Queue"). A closed, fixed technical taxonomy -- unlike
/// <see cref="Connector.IntegrationCategory"/>, which is a validated string precisely
/// because business modules name their own integration categories, this document's
/// own Endpoint section names exactly six concrete communication mechanisms with no
/// stated extension point.
/// </summary>
public enum EndpointType
{
    RestApi = 0,
    Soap,
    GraphQl,
    Webhook,
    Sftp,
    MessageQueue,
}

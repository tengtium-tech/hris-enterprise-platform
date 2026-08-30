namespace Hris.Foundation.Events.Domain;

/// <summary>
/// The three kinds event-framework.md's Event Categories section names: Domain
/// Events ("significant business activities"), Integration Events ("communication
/// with external systems"), and Platform Events ("platform activities"). Matches
/// docs/02-architecture/04-domain-driven-design/domain-events.md's own "Domain Event
/// vs Integration Event" distinction: "Not every Domain Event becomes an Integration
/// Event... derived from Domain Events when external communication is required."
/// </summary>
public enum EventCategory
{
    DomainEvent = 0,
    IntegrationEvent,
    PlatformEvent,
}

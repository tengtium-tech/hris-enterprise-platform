using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Identity of the <see cref="Connector"/> Aggregate Root. Source:
/// docs/03-foundation/integration-framework.md, Core Concepts ("A Connector
/// encapsulates integration logic").
/// </summary>
public readonly record struct ConnectorId(Guid Value) : IStronglyTypedId;

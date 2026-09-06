using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Identity of one <see cref="DataMapping"/> child Entity within a
/// <see cref="Connector"/>'s own <see cref="Connector.Mappings"/> collection.
/// </summary>
public readonly record struct DataMappingId(Guid Value) : IStronglyTypedId;

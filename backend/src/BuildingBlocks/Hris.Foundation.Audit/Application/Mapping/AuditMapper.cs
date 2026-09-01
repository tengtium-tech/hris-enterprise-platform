using Hris.Foundation.Audit.Application.Dtos;
using Hris.Foundation.Audit.Domain;

namespace Hris.Foundation.Audit.Application.Mapping;

/// <summary>
/// Maps <see cref="AuditRecord"/> to its query-side DTO, by hand rather than through a
/// registered Mapster profile -- the identical deviation every other Sprint 3
/// framework's own mapper states and justifies.
/// </summary>
internal static class AuditMapper
{
    public static AuditRecordDto ToDto(this AuditRecord record) => new(
        record.Id.Value,
        record.TimestampUtc,
        record.ActorId?.Value,
        record.Category.ToString(),
        record.Action,
        record.BusinessEntity,
        record.EntityIdentifier,
        record.PreviousValue,
        record.NewValue,
        record.SourceSystem,
        record.ClientApplication,
        record.IpAddress,
        record.DeviceInformation,
        record.CorrelationId?.Value,
        record.Outcome.ToString(),
        record.Metadata);
}

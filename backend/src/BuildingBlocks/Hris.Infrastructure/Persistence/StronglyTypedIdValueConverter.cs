using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hris.Infrastructure.Persistence;

/// <summary>
/// The one reusable EF Core Value Converter dbcontext-design.md calls for --
/// "Configure Strongly Typed IDs using reusable Value Converters... Strongly Typed IDs
/// remain Domain concepts while PostgreSQL stores native uuid values" -- shared by
/// every Foundation framework's and business module's own entity configuration rather
/// than each hand-writing its own converter per id type.
///
/// Generic over <typeparamref name="TId"/> rather than using reflection to discover a
/// <c>TId(Guid)</c> constructor: C#'s <c>new()</c> generic constraint only supports a
/// parameterless constructor, so the owning entity configuration supplies its own id's
/// factory explicitly -- e.g.
/// <code>new StronglyTypedIdValueConverter&lt;ConfigurationId&gt;(v => new ConfigurationId(v))</code>
/// -- which stays a compile-time-checked delegate instead of a reflection call that
/// only fails at runtime if a future id type's shape ever changes.
/// </summary>
public sealed class StronglyTypedIdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId
{
    public StronglyTypedIdValueConverter(Func<Guid, TId> fromGuid)
        : base(id => id.Value, value => fromGuid(value))
    {
        ArgumentNullException.ThrowIfNull(fromGuid);
    }
}

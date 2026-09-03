using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Identity of the <see cref="StoredFile"/> Aggregate Root. Source:
/// docs/03-foundation/file-storage.md, File Metadata ("File Identifier").
/// </summary>
public readonly record struct StoredFileId(Guid Value) : IStronglyTypedId;

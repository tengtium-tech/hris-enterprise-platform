using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Identity of the <see cref="FileVersion"/> child Entity. Source:
/// docs/03-foundation/file-storage.md, File Versioning ("Version History").
/// </summary>
public readonly record struct FileVersionId(Guid Value) : IStronglyTypedId;

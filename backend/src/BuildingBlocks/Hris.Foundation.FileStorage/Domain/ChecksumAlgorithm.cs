namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Source: docs/03-foundation/file-storage.md, File Integrity ("Checksum Validation").
/// The document names no specific algorithm; SHA-256 is this framework's own concrete
/// choice as the platform's single supported algorithm for now -- a closed set with one
/// member, the same shape <see cref="StorageProviderType"/> and every other Sprint 3/4
/// enum in this codebase already use, room to grow rather than a hardcoded literal
/// scattered through <see cref="Checksum"/>.
/// </summary>
public enum ChecksumAlgorithm
{
    Sha256 = 0,
}

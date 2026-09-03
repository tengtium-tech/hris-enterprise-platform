namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Source: docs/03-foundation/file-storage.md, Storage Provider ("The framework should
/// support multiple providers... Storage providers should be interchangeable").
/// </summary>
public enum StorageProviderType
{
    Local = 0,
    AmazonS3 = 1,
    AzureBlobStorage = 2,
    GoogleCloudStorage = 3,
    MinIO = 4,
    NetworkFileShare = 5,
    EnterpriseObjectStorage = 6,
}

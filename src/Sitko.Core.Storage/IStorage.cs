using Sitko.Core.Storage.Internal;

namespace Sitko.Core.Storage;

public interface IStorageInstance
{
}

public interface IStorage : IStorageInstance
{
    bool IsDefault { get; }

    /// <summary>
    ///     Upload file to storage
    /// </summary>
    /// <param name="file">Stream of data to upload</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="path">Path on storage to upload file into</param>
    /// <param name="metadata">Serializable object with file metadata</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>StorageItem with information about uploaded file</returns>
    Task<StorageItem> SaveAsync(Stream file, string fileName, string path,
        object? metadata = null, CancellationToken cancellationToken = default);

    Task<StorageItem> UpdateMetaDataAsync(StorageItem item, string fileName,
        object? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get uploaded file info without downloading file
    /// </summary>
    /// <param name="filePath">Full path to file in storage</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>StorageItem with information about uploaded file</returns>
    Task<StorageItem?> GetAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get uploaded file info and stream
    /// </summary>
    /// <param name="filePath">Full path to file in storage</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>DownloadResult with StorageItem and Stream</returns>
    Task<DownloadResult?> DownloadAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete file from storage
    /// </summary>
    /// <param name="filePath">Full path to file in storage</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if success</returns>
    Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if file exists in storage
    /// </summary>
    /// <param name="filePath">Full path to file in storage</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if file exists</returns>
    Task<bool> IsExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete all files from storage. Specific to storage realization.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     List folders and files in specified path
    /// </summary>
    /// <param name="path">Path to list</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>List of StorageNode</returns>
    Task<IEnumerable<StorageNode>> GetDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Refreshes storage items tree and returns folders and files in specified path
    /// </summary>
    /// <param name="path">Path to list</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>List of StorageNode</returns>
    Task<IEnumerable<StorageNode>> RefreshDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generate public uri for file
    /// </summary>
    /// <param name="item">StorageItem to generate uri for</param>
    /// <returns>Public URI</returns>
    Uri PublicUri(StorageItem item);

    /// <summary>
    ///     Generate public uri for file
    /// </summary>
    /// <param name="filePath">Path to file in storage</param>
    /// <returns>Public URI</returns>
    Uri PublicUri(string filePath);

    public Task<IEnumerable<StorageItemInfo>> GetAllItemsAsync(string path,
        CancellationToken cancellationToken = default);
}

// Generic interface is required for dependency injection
// ReSharper disable once UnusedTypeParameter
public interface IStorage<TStorageOptions> : IStorage where TStorageOptions : StorageOptions
{
}


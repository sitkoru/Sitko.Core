using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    public interface IStorage
    {
        /// <summary>
        /// Upload file to storage
        /// </summary>
        /// <param name="file">Stream of data to upload</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="path">Path on storage to upload file into</param>
        /// <param name="metadata">Serializable object with file metadata</param>
        /// <returns>StorageItem with information about uploaded file</returns>
        Task<StorageItem> SaveAsync(Stream file, string fileName, string path,
            object? metadata = null);

        /// <summary>
        /// Get uploaded file info without downloading file
        /// </summary>
        /// <param name="filePath">Full path to file in storage</param>
        /// <returns>StorageItem with information about uploaded file</returns>
        Task<StorageItem?> GetAsync(string filePath);

        /// <summary>
        /// Get uploaded file info and stream
        /// </summary>
        /// <param name="filePath">Full path to file in storage</param>
        /// <returns>DownloadResult with StorageItem and Stream</returns>
        Task<DownloadResult?> DownloadAsync(string filePath);

        /// <summary>
        /// Delete file from storage
        /// </summary>
        /// <param name="filePath">Full path to file in storage</param>
        /// <returns>True if success</returns>
        Task<bool> DeleteAsync(string filePath);

        /// <summary>
        /// Check if file exists in storage
        /// </summary>
        /// <param name="filePath">Full path to file in storage</param>
        /// <returns>True if file exists</returns>
        Task<bool> IsExistsAsync(string filePath);

        /// <summary>
        /// Delete all files from storage. Specific to storage realization.
        /// </summary>
        /// <returns></returns>
        Task DeleteAllAsync();

        /// <summary>
        /// List folders and files in specified path
        /// </summary>
        /// <param name="path">Path to list</param>
        /// <returns>List of StorageNode</returns>
        Task<IEnumerable<StorageNode>> GetDirectoryContentsAsync(string path);
        
        /// <summary>
        /// Refreshes storage items tree and returns folders and files in specified path
        /// </summary>
        /// <param name="path">Path to list</param>
        /// <returns>List of StorageNode</returns>
        Task<IEnumerable<StorageNode>> RefreshDirectoryContentsAsync(string path);

        /// <summary>
        /// Generate public uri for file
        /// </summary>
        /// <param name="item">StorageItem to generate uri for</param>
        /// <returns>Public URI</returns>
        Uri PublicUri(StorageItem item);

        /// <summary>
        /// Generate public uri for file
        /// </summary>
        /// <param name="filePath">Path to file in storage</param>
        /// <returns>Public URI</returns>
        Uri PublicUri(string filePath);
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IStorage<T> : IStorage where T : StorageOptions
    {
    }
}

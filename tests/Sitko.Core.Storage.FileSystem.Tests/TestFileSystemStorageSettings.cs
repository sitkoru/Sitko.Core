namespace Sitko.Core.Storage.FileSystem.Tests
{
    public class TestFileSystemStorageSettings : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "/tmp/storage";
        public override string Name { get; set; } = "test_file_storage";
    }
}

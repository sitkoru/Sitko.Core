namespace Sitko.Core.Storage.FileSystem;

public class
    FileSystemStorageModule<TStorageOptions> : StorageModule<FileSystemStorage<TStorageOptions>, TStorageOptions>
    where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
{
    public override string OptionsKey => $"Storage:FileSystem:{typeof(TStorageOptions).Name}";
    public override string[] OptionKeys => new[] { "Storage:FileSystem:Default", OptionsKey };
}

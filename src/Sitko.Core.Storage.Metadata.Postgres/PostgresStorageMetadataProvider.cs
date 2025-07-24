using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Metadata.Postgres.DB;
using Sitko.Core.Storage.Metadata.Postgres.DB.Models;

namespace Sitko.Core.Storage.Metadata.Postgres;

public class
    PostgresStorageMetadataProvider<TStorageOptions> : BaseStorageMetadataProvider<
    PostgresStorageMetadataModuleOptions<TStorageOptions>, TStorageOptions>
    where TStorageOptions : StorageOptions
{
    private readonly IDbContextFactory<StorageDbContext> dbContextFactory;

    public PostgresStorageMetadataProvider(
        IOptionsMonitor<PostgresStorageMetadataModuleOptions<TStorageOptions>> options,
        IOptionsMonitor<TStorageOptions> storageOptions,
        IDbContextFactory<StorageDbContext> dbContextFactory,
        ILogger<PostgresStorageMetadataProvider<TStorageOptions>> logger) : base(options, storageOptions, logger) =>
        this.dbContextFactory = dbContextFactory;

    private StorageDbContext GetDbContext() => dbContextFactory.CreateDbContext();

    //return new(Options.CurrentValue.GetConnectionString(), Options.CurrentValue.Schema);
    private async Task<StorageItemRecord?> GetItemRecordAsync(StorageDbContext dbContext, string filePath,
        CancellationToken cancellationToken = default) =>
        await dbContext.Records.FirstOrDefaultAsync(r =>
                r.Storage == StorageOptions.CurrentValue.Name && r.FilePath == filePath,
            cancellationToken);

    protected override async Task DoDeleteMetadataAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = GetDbContext();
        var record = await GetItemRecordAsync(dbContext, filePath, cancellationToken);
        if (record is not null)
        {
            dbContext.Records.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    protected override async Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = GetDbContext();
        await dbContext.Set<StorageItemRecord>().Where(record => record.Storage == StorageOptions.CurrentValue.Name)
            .ExecuteDeleteAsync(cancellationToken);
    }

    protected override async Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = GetDbContext();
        if (path.StartsWith('/'))
        {
            path = path.Substring(1);
        }

        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var records = await dbContext.Records
            .Where(r => r.Storage == StorageOptions.CurrentValue.Name && r.Path.StartsWith(path))
            .ToListAsync(cancellationToken);

        var root = StorageNode.CreateDirectory("/", "/");
        foreach (var itemRecord in records)
        {
            // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage
            root.AddItem(itemRecord.StorageItem);
        }

        var parts = PreparePath(path.Trim('/'))!.Split("/");
        var current = root;
        foreach (var part in parts)
        {
            current = current?.Children.Where(n => n.Type == StorageNodeType.Directory)
                .FirstOrDefault(f => f.Name == part);
        }

        return current?.Children ?? Array.Empty<StorageNode>();
    }

    private static string? PreparePath(string? path) => path?.Replace("\\", "/").Replace("//", "/");

    protected override async Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = GetDbContext();
        var record = await GetItemRecordAsync(dbContext, path, cancellationToken);
        return record?.Metadata;
    }

    protected override async Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
        bool isNew = true,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = GetDbContext();
        var record = await GetItemRecordAsync(dbContext, storageItem.FilePath, cancellationToken);
        if (record is null)
        {
            record = new StorageItemRecord(StorageOptions.CurrentValue.Name, storageItem);
            await dbContext.Records.AddAsync(record, cancellationToken);
        }

        if (metadata?.FileName != null)
        {
            record.FileName = metadata.FileName;
        }

        record.Metadata = metadata;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    protected override async Task DoInitAsync(CancellationToken cancellationToken = default)
    {
        await base.DoInitAsync(cancellationToken);
        Logger.LogDebug("Migrate Storage metadata database");
        await using var dbContext = GetDbContext();
        await dbContext.Database.MigrateAsync(cancellationToken);
        Logger.LogDebug("Storage metadata database migrated");
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Results;
using Sitko.Core.Storage.Remote.Server;
using Sitko.Core.Storage.Tests;

namespace Sitko.Core.Storage.Remote.Tests.Server;

[Route("/Upload")]
public class UploadController : BaseRemoteStorageController<TestS3StorageSettings, FileMetaData>
{
    public UploadController(IStorage<TestS3StorageSettings> storage, ILogger<UploadController> logger) : base(
        storage, logger)
    {
    }

    protected override async Task<IOperationResult> CanReadAsync(string path, HttpRequest request) =>
        new OperationResult();

    protected override async Task<IOperationResult> CanDeleteAsync(string? path, HttpRequest request) =>
        new OperationResult();

    protected override async Task<IOperationResult> CanListAsync(string? path, HttpRequest request) =>
        new OperationResult();

    protected override async Task<IOperationResult>
        CanUploadAsync(UploadStorageItem<FileMetaData> uploadStorageItem, HttpRequest request) =>
        new OperationResult();
}

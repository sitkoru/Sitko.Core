using Microsoft.AspNetCore.Mvc;
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
}


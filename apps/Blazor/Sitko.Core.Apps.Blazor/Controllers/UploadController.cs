using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Apps.Blazor.Client.Pages;
using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote.Server;

namespace Sitko.Core.Apps.Blazor.Controllers;

[Route("/Upload")]
public class UploadController : BaseRemoteStorageController<TestBlazorStorageOptions, BarStorageMetadata>
{
    public UploadController(IStorage<TestBlazorStorageOptions> storage, ILogger<UploadController> logger) : base(
        storage, logger)
    {
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitko.Core.Apps.MudBlazorDemo.Pages;
using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote.Server;

namespace Sitko.Core.Apps.MudBlazorDemo.Controllers;

[Route("/Upload")]
public class UploadController : BaseRemoteStorageController<TestBlazorStorageOptions, BarStorageMetadata>
{
    public UploadController(IStorage<TestBlazorStorageOptions> storage, ILogger<UploadController> logger) : base(
        storage, logger)
    {
    }
}

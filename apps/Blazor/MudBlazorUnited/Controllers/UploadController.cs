using Microsoft.AspNetCore.Mvc;
using MudBlazorUnited.Components.Pages;
using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote.Server;

namespace MudBlazorUnited.Controllers;

[Route("/Upload")]
public class UploadController : BaseRemoteStorageController<TestBlazorStorageOptions, BarStorageMetadata>
{
    public UploadController(IStorage<TestBlazorStorageOptions> storage, ILogger<UploadController> logger) : base(
        storage, logger)
    {
    }
}

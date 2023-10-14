using Microsoft.AspNetCore.Mvc;
using MudBlazorAuto.Data.Entities;
using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote.Server;

namespace MudBlazorAuto.Controllers;

[Route("/Upload")]
public class UploadController : BaseRemoteStorageController<TestBlazorStorageOptions, BarStorageMetadata>
{
    public UploadController(IStorage<TestBlazorStorageOptions> storage, ILogger<UploadController> logger) : base(
        storage, logger)
    {
    }
}

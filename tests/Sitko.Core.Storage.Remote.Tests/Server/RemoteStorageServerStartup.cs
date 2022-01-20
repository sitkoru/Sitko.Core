using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;

namespace Sitko.Core.Storage.Remote.Tests.Server;

public class RemoteStorageServerStartup : BaseStartup
{
    public RemoteStorageServerStartup(IConfiguration configuration, IHostEnvironment environment) : base(
        configuration, environment)
    {
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Storage.Remote.Tests.Server;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.Remote.Tests;

public class BaseRemoteStorageTestScope : BaseTestScope
{
    private IHost? host;
    private TestServer? server;

    public override async Task BeforeConfiguredAsync()
    {
        var application = new RemoteStorageServerApplication(Array.Empty<string>());
        host = await application.StartAsync();
        server = host.GetTestServer();
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddRemoteStorage<TestRemoteStorageSettings>(moduleOptions =>
        {
            moduleOptions.PublicUri = new Uri("https://localhost");
            if (server is not null)
            {
                moduleOptions.RemoteUrl = new Uri(server.BaseAddress, "Upload/");
                moduleOptions.HttpClientFactory = () =>
                {
                    var client = server.CreateClient();
                    client.BaseAddress = new Uri(client.BaseAddress!, "Upload/");
                    return client;
                };
            }
        });

        return application;
    }

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        var storage = GetService<IStorage<TestRemoteStorageSettings>>();
        await storage.DeleteAllAsync();
        if (host is not null)
        {
            await host.StopAsync();
            host.Dispose();
        }
    }
}

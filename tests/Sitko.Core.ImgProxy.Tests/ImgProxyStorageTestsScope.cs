using Microsoft.Extensions.Hosting;
using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.ImgProxy;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyStorageTestsScope : ImgProxyTestsScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddFileSystemStorage<ImgProxyFileSystemStorageSettings>(
                settings =>
                {
                    settings.PublicUri = new Uri("https://img.test.com");
                })
            .AddImgProxyStorage<ImgProxyFileSystemStorageSettings>();
}

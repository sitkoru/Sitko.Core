using Sitko.Core.Storage.FileSystem;
using Sitko.Core.Storage.ImgProxy;
using Sitko.Core.Xunit;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyStorageTestsScope : ImgProxyTestsScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddFileSystemStorage<ImgProxyFileSystemStorageSettings>(settings =>
        {
            settings.PublicUri = new Uri("https://img.test.com");
        });
        application.AddImgProxyStorage<ImgProxyFileSystemStorageSettings>();
        return application;
    }
}


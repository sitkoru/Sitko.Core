using Sitko.Core.Xunit;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyTestsScope : BaseTestScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddImgProxy(options =>
        {
            options.Host = "https://imgproxy.test.com";
            options.Key = "1234";
            options.Salt = "4567";
            options.EncodeUrls = true;
        });
        return application;
    }
}


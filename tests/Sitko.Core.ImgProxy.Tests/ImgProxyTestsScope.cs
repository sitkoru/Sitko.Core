using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyTestsScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name).AddImgProxy(options =>
        {
            options.Host = "https://imgproxy.test.com";
            options.Key = "1234";
            options.Salt = "4567";
            options.EncodeUrls = true;
        });
}

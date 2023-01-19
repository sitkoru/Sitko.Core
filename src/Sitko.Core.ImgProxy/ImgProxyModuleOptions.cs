using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.ImgProxy;

[PublicAPI]
public class ImgProxyModuleOptions : BaseModuleOptions
{
    public string Host { get; set; } = "";
    public string Key { get; set; } = "";
    public string Salt { get; set; } = "";
    public bool EncodeUrls { get; set; }
    public bool DisableProxy { get; set; }
}


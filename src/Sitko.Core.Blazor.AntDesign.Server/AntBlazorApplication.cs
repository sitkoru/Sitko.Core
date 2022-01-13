using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Blazor.Server;

namespace Sitko.Core.Blazor.AntDesign.Server;

public class AntBlazorApplication<TStartup> : BlazorApplication<TStartup> where TStartup : AntBlazorStartup
{
    public AntBlazorApplication(string[] args) : base(args) => this.AddBlazorFileUpload();
}

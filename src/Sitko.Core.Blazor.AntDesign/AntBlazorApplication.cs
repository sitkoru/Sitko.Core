using Sitko.Core.App.Blazor;
using Sitko.Core.Blazor.FileUpload;

namespace Sitko.Core.Blazor.AntDesignComponents
{
    public class AntBlazorApplication<TStartup> : BlazorApplication<TStartup> where TStartup : AntBlazorStartup
    {
        public AntBlazorApplication(string[] args) : base(args)
        {
            this.AddBlazorFileUpload();
        }
    }
}

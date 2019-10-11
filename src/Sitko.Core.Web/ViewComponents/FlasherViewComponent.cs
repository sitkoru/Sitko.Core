using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sitko.Core.Web.Components;

namespace Sitko.Core.Web.ViewComponents
{
    public class FlasherViewComponent : ViewComponent
    {
        private readonly Flasher _flasher;

        public FlasherViewComponent(Flasher flasher)
        {
            _flasher = flasher;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return await Task.FromResult(View(_flasher));
        }
    }
}

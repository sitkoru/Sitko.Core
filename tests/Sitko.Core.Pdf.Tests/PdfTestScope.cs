using Sitko.Core.App;
using Sitko.Core.Xunit;

namespace Sitko.Core.Pdf.Tests
{
    public class PdfTestScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name);
            application.AddModule<TestApplication, PdfRendererModule, PdfRendererModuleConfig>();
            return application;
        }
    }
}

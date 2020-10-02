using Sitko.Core.Xunit;

namespace Sitko.Core.Pdf.Tests
{
    public class PdfTestScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name).AddModule<PdfRendererModule, PdfRendererModuleConfig>();
        }
    }
}

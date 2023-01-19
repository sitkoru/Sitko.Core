using Sitko.Core.Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Pdf.Tests;

public class BasePdfTest : BaseTest<PdfTestScope>
{
    public BasePdfTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}


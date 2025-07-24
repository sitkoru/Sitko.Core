using FluentAssertions;
using Razor.Templating.Core;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Web.Tests;

public class BasicTest : BaseTest
{
    public BasicTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ViewToHtmlTest()
    {
        var model = new TestViewModel();
        var html = await RazorTemplateEngine.RenderAsync("~/Views/View.cshtml", model);
        html.Should().Contain(model.Id.ToString());
    }
}

public class TestViewModel
{
    public Guid Id { get; } = Guid.NewGuid();
}

using Microsoft.Extensions.Hosting;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.Xunit;

namespace Sitko.Core.ElasticStack.Tests;

public class ElasticStackScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name).AddElasticStack(options =>
        {
            options.ElasticSearchUrls = new List<Uri> { new("http://localhost:9200") };
            options.ApmServerUrls = new List<Uri> { new("http://localhost:8500") };
            options.LoggingTemplateVersion = AutoRegisterTemplateVersion.ESv8;
            options.LoggingIndexFormat = "logs-test";
            options.EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.RaiseCallback;
            options.FailureCallback = e => Console.WriteLine("Unable to submit event to elastic: " + e.MessageTemplate);
        });
}

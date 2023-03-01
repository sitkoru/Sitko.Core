using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.App.Web;
using Sitko.Core.Xunit;

namespace Sitko.Core.ElasticStack.Tests;

public class ElasticStackScope : BaseTestScope<ElasticApplication>
{
}

public class ElasticStartup : BaseStartup
{
    public ElasticStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
        environment)
    {
    }
}

public class ElasticApplication : WebApplication<ElasticStartup>
{
    public ElasticApplication(string[] args) : base(args) => this.AddElasticStack(options =>
    {
        options.ElasticSearchUrls = new List<Uri> { new("http://localhost:9200") };
        options.LoggingTemplateVersion = AutoRegisterTemplateVersion.ESv8;
        options.LoggingIndexFormat = "logs-test";
        options.EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.RaiseCallback;
        options.FailureCallback = e => Console.WriteLine("Unable to submit event to elastic: " + e.MessageTemplate);
    });
}

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using Xunit;

namespace Sitko.Core.Search.OpenSearch.Tests;

public class OpenSearchClientLifetimeTests
{
    [Fact]
    public async Task SearchesInDifferentScopesAndModelsReuseClientAndPreserveRequests()
    {
        var options = new MutableOptions();
        var response = Encoding.UTF8.GetBytes("""{"hits":{"total":{"value":1,"relation":"eq"},"hits":[{"_id":"1","_source":{"id":"1","title":"Found"}}]}}""");
        var connection = new CapturingConnection(response);
        var created = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptionsMonitor<OpenSearchModuleOptions>>(options);
        new TestOpenSearchModule().Register(services);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(OpenSearchClientProvider)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ISearcher<>)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        services.AddSingleton(_ => new OpenSearchClientProvider(options, NullLogger.Instance, uri =>
        {
            Interlocked.Increment(ref created);
            return new ConnectionSettings(uri, connection);
        }));
        using var root = services.BuildServiceProvider();
        using var start = new ManualResetEventSlim();
        var searches = Enumerable.Range(0, 16).Select(i => Task.Run(async () =>
        {
            start.Wait(TestContext.Current.CancellationToken);
            using var scope = root.CreateScope();
            if (i % 2 == 0)
            {
                var searcher = scope.ServiceProvider.GetRequiredService<ISearcher<TestSearchModel>>();
                var result = await searcher.SearchAsync("items", "needle", null, TestContext.Current.CancellationToken);
                Assert.Equal("Found", Assert.Single(result).SearchModel.Title);
            }
            else
            {
                var searcher = scope.ServiceProvider.GetRequiredService<ISearcher<OtherSearchModel>>();
                var result = await searcher.SearchAsync("items", "needle", null, TestContext.Current.CancellationToken);
                Assert.Equal("Found", Assert.Single(result).SearchModel.Title);
            }
        })).ToArray();
        start.Set();
        await Task.WhenAll(searches);
        Assert.Equal(1, created);
        Assert.Equal(16, connection.Requests.Count);
        Assert.All(connection.Requests, request =>
        {
            using var json = JsonDocument.Parse(request);
            Assert.Equal("needle", json.RootElement.GetProperty("query").GetProperty("query_string")
                .GetProperty("query").GetString());
        });

        var provider = root.GetRequiredService<OpenSearchClientProvider>();
        var previous = provider.GetClient();
        options.CurrentValue = new OpenSearchModuleOptions { Url = "http://new.example:9200", Login = "user", Password = "password" };
        var updated = provider.GetClient();
        Assert.NotSame(previous, updated);
        Assert.Same(updated, provider.GetClient());
        Assert.Equal(2, created);
        var updatedResponse = await updated.SearchAsync<TestSearchModel>(s => s.Index("items"), TestContext.Current.CancellationToken);
        Assert.Equal("new.example", updatedResponse.ApiCall.Uri.Host);
        var credentials = ((IConnectionConfigurationValues)updated.ConnectionSettings).BasicAuthenticationCredentials;
        Assert.Equal("user", credentials.Username);
        Assert.Equal("password", SecureStrings.CreateString(credentials.Password));
        options.CurrentValue = new OpenSearchModuleOptions { Url = "http://new.example:9200", Login = "user", Password = "other" };
        var passwordChanged = provider.GetClient();
        Assert.NotSame(updated, passwordChanged);
        Assert.Equal(3, created);
        options.CurrentValue = new OpenSearchModuleOptions { Url = "http://new.example:9200", Login = "user", Password = "other", DisableCertificatesValidation = true };
        Assert.NotSame(passwordChanged, provider.GetClient());
        Assert.Equal(4, created);
        var oldResponse = await previous.SearchAsync<TestSearchModel>(s => s.Index("items"), TestContext.Current.CancellationToken);
        Assert.Equal("Found", Assert.Single(oldResponse.Hits).Source.Title);
    }

    private sealed class TestOpenSearchModule : OpenSearchModule
    {
        public void Register(IServiceCollection services) => ConfigureSearch(services);
    }

    public sealed class OtherSearchModel : BaseSearchModel;

    private sealed class CapturingConnection(byte[] response) : InMemoryConnection(response, 200)
    {
        public ConcurrentBag<string> Requests { get; } = [];

        public override async Task<TResponse> RequestAsync<TResponse>(RequestData requestData, CancellationToken cancellationToken)
        {
            if (requestData.PostData != null)
            {
                using var stream = new MemoryStream();
                await requestData.PostData.WriteAsync(stream, requestData.ConnectionSettings, cancellationToken);
                Requests.Add(Encoding.UTF8.GetString(stream.ToArray()));
            }

            return await base.RequestAsync<TResponse>(requestData, cancellationToken);
        }
    }

    private sealed class MutableOptions : IOptionsMonitor<OpenSearchModuleOptions>
    {
        public OpenSearchModuleOptions CurrentValue { get; set; } = new();
        public OpenSearchModuleOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<OpenSearchModuleOptions, string?> listener) => null;
    }
}

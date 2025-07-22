using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.Storage.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.S3.Tests;

public class BasicTests : BasicTests<BaseS3StorageTestScope>
{
    public BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task HealthCheck()
    {
        var scope = await GetScopeAsync();

        var storage = scope.GetService<IStorage>();

        Assert.NotNull(storage);

        const string fileName = "file.txt";
        const string path = "upload";
        await using (var file = File.Open("Data/file.txt", FileMode.Open))
        {
            await storage.SaveAsync(file, fileName, path);
        }

        var s3BucketHealthCheck = scope.GetService<S3BucketHealthCheck<TestS3StorageSettings>>();
        var bucketHcResult = await s3BucketHealthCheck.CheckHealthAsync(new HealthCheckContext());
        bucketHcResult.Status.Should().Be(HealthStatus.Healthy);
    }
}

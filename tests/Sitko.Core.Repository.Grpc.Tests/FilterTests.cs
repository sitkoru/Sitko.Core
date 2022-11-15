using FluentAssertions;
using Sitko.Core.App.Json;
using Sitko.Core.Grpc;
using Xunit;

namespace Sitko.Core.Repository.Grpc.Tests;

public class FilterTests
{
    [Fact]
    public void TestJson()
    {
        var condition = new QueryContextCondition("test", QueryContextOperator.Equal, "123");
        var json = JsonHelper.SerializeWithMetadata(condition);
        var fromJson = JsonHelper.DeserializeWithMetadata<QueryContextCondition>(json);
        fromJson.Should().Be(condition);
    }

    [Fact]
    public void SetGroup()
    {
        var group = new QueryContextConditionsGroup(
            new QueryContextCondition("test", QueryContextOperator.Equal, "123"));
        var request = new TestRequest();
        request.SetFilter(group);

        CheckFilter(request, new[] { group });
    }

    [Fact]
    public void SetGroups()
    {
        var groups = new[]
        {
            new QueryContextConditionsGroup(new QueryContextCondition("test", QueryContextOperator.Equal, "123")),
            new QueryContextConditionsGroup(new QueryContextCondition("test", QueryContextOperator.NotEqual, "456"))
        };
        var request = new TestRequest();
        request.SetFilter(groups);

        CheckFilter(request, groups);
    }

    [Fact]
    public void SetCondition()
    {
        var condition = new QueryContextCondition("test", QueryContextOperator.Equal, "123");
        var request = new TestRequest();
        request.SetFilter(condition);

        CheckFilter(request, new[] { new QueryContextConditionsGroup(condition) });
    }

    [Fact]
    public void SetConditionSimple()
    {
        var request = new TestRequest();
        request.SetFilter("test", QueryContextOperator.Equal, "123");

        CheckFilter(request,
            new[]
            {
                new QueryContextConditionsGroup(
                    new QueryContextCondition("test", QueryContextOperator.Equal, "123"))
            });
    }

    private static void CheckFilter(TestRequest request, QueryContextConditionsGroup[] sourceFilter)
    {
        request.RequestInfo.Filter.Should().NotBeEmpty();
        var filter = request.RequestInfo.GetFilter();
        filter.Should().NotBeEmpty();
        filter.Should().Equal(sourceFilter);
    }
}

public class TestRequest : IGrpcRequest
{
    public ApiRequestInfo RequestInfo { get; set; } = new();
}


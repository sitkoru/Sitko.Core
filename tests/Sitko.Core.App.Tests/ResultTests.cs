using FluentAssertions;
using Sitko.Core.App.Results;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Tests;

public class ResultTests : BaseTest
{
    public ResultTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void Success()
    {
        var result = new OperationResult();
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Error()
    {
        var result = new OperationResult("Error");
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Error");
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void SetError()
    {
        var result = new OperationResult();
        result.IsSuccess.Should().BeTrue();
        result.SetError("Error");
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Error");
    }

    [Fact]
    public void SetException()
    {
        var ex = new InvalidOperationException("Exception");
        var result = new OperationResult();
        result.IsSuccess.Should().BeTrue();
        result.SetException(ex);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Exception");
        result.Exception.Should().Be(ex);
    }

    [Fact]
    public void Exception()
    {
        var ex = new InvalidOperationException("Exception");
        var result = new OperationResult(ex);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Exception");
        result.Exception.Should().Be(ex);
    }

    [Fact]
    public void Result()
    {
        var result = new OperationResult<Foo>(new Foo(1));
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.Value.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void ResultError()
    {
        var result = new OperationResult<Foo>("Error");
        result.IsSuccess.Should().BeFalse();
        result.Result.Should().BeNull();
        result.ErrorMessage.Should().Be("Error");
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void ResultException()
    {
        var ex = new InvalidOperationException("Exception");
        var result = new OperationResult<Foo>(ex);
        result.IsSuccess.Should().BeFalse();
        result.Result.Should().BeNull();
        result.ErrorMessage.Should().Be("Exception");
        result.Exception.Should().Be(ex);
    }
}

public class Foo
{
    public Foo(int value) => Value = value;
    public int Value { get; }
}

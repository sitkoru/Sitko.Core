using Grpc.Core;

namespace Sitko.Core.Apps.Grpc.Services;

public class GreeterService(ILogger<GreeterService> logger) : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context) =>
        Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
}

public class FooService : Grpc.FooService.FooServiceBase
{
    public override Task<FooReply> Foo(FooRequest request, ServerCallContext context) =>
        Task.FromResult(new FooReply { Baz = "Baz " + request.Bar });
}

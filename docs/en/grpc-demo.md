# Local gRPC demo with Consul

[Русский](../ru/grpc-demo.md)

The [apps/gRPC](../../apps/gRPC/README.md) application hosts both the server
and the client. An HTTPS request to `/Test` makes a real `greet.FooService/Foo`
RPC with `Bar=123` through Microsoft Service Discovery and its own Consul DNS.
The response is `Baz 123`. No direct client address or fallback bypassing Consul
is configured.

## Prerequisites

Install the .NET 10 SDK specified in [global.json](../../global.json), Docker
with Compose, `curl` and `dig`. Run commands from the repository root.

The `CurrentUser/My` certificate store must contain a valid, trusted `localhost`
certificate with its private key, prepared with standard `dotnet dev-certs`.
Check it before starting:

```bash
dotnet dev-certs https --check --trust
```

If this check fails, prepare the certificate and trust for your OS first.
The demo does not install certificates or change trust. It loads the certificate
with `CertificateLoader.LoadFromStoreCert` and `allowInvalid=false`.
The gRPC client validates the server certificate.

[AppWebConfigurationModule](../../src/Sitko.Core.App.Web/AppWebConfigurationModule.cs)
generates its own self-signed certificate for named TLS listeners. The demo
presents the trusted certificate through `ConfigureHttpsDefaults` and
`OnAuthenticate`, following the [existing Kestrel example](../../tests/Sitko.Core.Grpc.Client.Tests.Host/Program.cs).
This preserves standard client certificate validation without changing the libraries.

| Purpose | Address |
| --- | --- |
| `/Test`, HTTPS HTTP/1 | `https://localhost:18080/Test` |
| gRPC, TLS HTTP/2 | `localhost:18081` |
| Consul HTTP | `http://127.0.0.1:18500` |
| Consul DNS, TCP and UDP | `127.0.0.1:18600` |

All demo listeners bind to loopback. These ports must be available.
[compose.yml](../../apps/gRPC/compose.yml) creates a separate `sitko-grpc-demo`
project with one development-mode Consul instance; its data is temporary.

## Start and check readiness

Start Consul and wait for a non-empty leader address:

```bash
docker compose -f apps/gRPC/compose.yml up -d
curl -fsS http://127.0.0.1:18500/v1/status/leader
```

Start the application in another terminal:

```bash
dotnet run --project apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj --launch-profile https
```

The application publishes `greet.Greeter` and `greet.FooService` with the
`grpc-demo` environment, the `localhost` address and the actual port `18081`.
Registration is asynchronous: a running process does not yet prove RPC readiness.
Check the registrations and the healthy DNS SRV response for `greet.FooService`:

```bash
curl -fsS http://127.0.0.1:18500/v1/agent/services
dig @127.0.0.1 -p 18600 grpc-e10cea5836abef17d515d08191aa0a5fb094324879d81d95579e6a6af0.service.consul SRV +short
```

The HTTP response must contain both contracts in `Meta.contract`, the `grpc-demo`
environment, `Address=localhost` and `Port=18081`. The Foo DNS response must contain
port `18081`. If the registration has not appeared yet, repeat the check after
registration. The DNS name is computed from the environment and full contract name.
If you change them, use the current `Service` value from `/v1/agent/services`
and append `.service.consul`.

Once the registration is available, request:

```bash
curl --http1.1 --fail-with-body --max-time 15 -i https://localhost:18080/Test
```

Expect HTTP `200` with the body `Baz 123`. The application log shows the route
`gRPC - /greet.FooService/Foo`: `/Test` returns the RPC result rather than a local
constant. `Grpc:Client:Default:DefaultDeadline` limits the RPC to ten seconds.
Resolve TLS failures by fixing the certificate and trust; disabling certificate
validation is outside this startup scenario.

## Check that Consul is required and stop

To check that there is no direct path, stop the application with `Ctrl+C`,
then stop only this demo's Consul:

```bash
docker compose -f apps/gRPC/compose.yml stop consul
```

Start the application with the same command and request `/Test` again.
It will not return a successful `Baz 123`: the fresh client cannot discover
the server. The process itself may start because the library retries registration
in the background. Use a fresh process for this check: an existing client may
retain its connection or DNS result.

Stop the application with `Ctrl+C`. To repeat the successful scenario, start
Consul with the `up -d` command above. To clean up the demo, run:

```bash
docker compose -f apps/gRPC/compose.yml down
```

This removes the resources of the `sitko-grpc-demo` project. A single process
hosting both client and server does not verify recovery across multiple instances,
performance or production readiness. See the [gRPC/SD guide](grpc-service-discovery.md)
for library behavior and additional settings.

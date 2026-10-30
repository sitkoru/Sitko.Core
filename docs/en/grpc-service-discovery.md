# gRPC and service discovery

[Русский](../ru/grpc-service-discovery.md)

`Sitko.Core.Grpc.Client` uses Microsoft Service Discovery and DNS SRV. `Sitko.Core.ServiceDiscovery.Consul` publishes server contracts and configures DNS. The gRPC client and server packages do not depend on Consul; applications add the provider separately.

## Registration

Register server implementations through the fluent API:

```csharp
var app = builder.AddSitkoCoreWeb();
app.AddConsulServiceDiscovery();
app.AddGrpcServer()
    .RegisterService<InvoiceGrpcService>()
    .RegisterService<ReportsGrpcService>();
```

The client only needs the generated protobuf type:

```csharp
var app = builder.AddSitkoCore();
app.AddConsulServiceDiscovery();
app.AddGrpcClient<Invoice.InvoiceClient>();
```

An application package can register its clients from its module registration extension. Register modules before `Build`; the current App does not support adding modules inside `ConfigureHostBuilder` while building the container. A client-only application does not require a web server.

The full protobuf name, such as `accounting.v1.Invoice`, identifies both the discovered contract and the named gRPC client. Equal short C# names in different protobuf packages remain distinct. Clients do not need the server application's name.

## Consul and server addresses

Example for separate processes on the same host:

```json
{
  "Application": {
    "Environment": "local",
    "Web": {
      "Ports": {
        "gRPC": {
          "Port": 0,
          "Protocol": "Http2",
          "UseTLS": true,
          "ExternalAddress": "127.0.0.1"
        }
      }
    }
  },
  "ServiceDiscovery": {
    "Consul": {
      "Address": "http://127.0.0.1:8500",
      "DnsServers": ["127.0.0.1:8600"]
    }
  }
}
```

The client uses the same `Application:Environment` and `ServiceDiscovery:Consul` settings, without `Web`. `Port=0` lets the operating system select a port. After Kestrel starts, discovery publishes the actual port of the named TLS/HTTP2 listener. `ExternalAddress` is the address advertised to clients; it does not restrict the listening interface. In Kubernetes, use a reachable Pod IP. Loopback is suitable only for processes on the same host.

Each contract of an application instance has its own Consul registration. Multiple contracts can share a TLS port. The exact environment/full-contract pair determines a common DNS label; the original values remain in metadata. Registration and TTL use the same configured agent. Consul DNS must exclude instances whose checks are critical.

Before application startup completes, discovery validates the actual positive port, HTTP/2 and advertised address. `ExternalAddress` cannot be a wildcard (`0.0.0.0`, `::`, `*`, `+`); errors name the contract. One background loop performs network registration after startup. Graceful shutdown removes only this instance’s registrations. After a crash, the record remains until Consul deletes it, but TTL expiry excludes it from healthy DNS responses. The existing client receives updated endpoints for subsequent RPCs; in-flight calls are not transferred.

`DnsServers` configures DNS addresses and ports independently of the agent's HTTP `Address`. The library does not change system DNS settings. Consul outages may cause RPC failures; retaining the last known endpoints is not guaranteed.

## Direct addresses and disabling discovery

One client registration supports both addressing modes:

```json
{
  "Grpc": {
    "Client": {
      "Contracts": {
        "accounting.v1.Invoice": {
          "Address": "https://invoices.example:7443"
        }
      }
    }
  }
}
```

A valid `Address` selects a direct call without requiring an SD provider. An absent address selects discovery. An invalid address, disabled discovery, or missing required provider fails startup with the contract name. A failed direct call does not fall back to discovery. Changing modes requires configuration and an application restart, without recompilation or another client registration method.

`ServiceDiscovery:Enabled=false` disables discovery, publication, and TTL. Direct clients and server RPCs continue to work. A standalone server does not require an SD provider. Explicitly adding `AddServiceDiscovery()` to publish server contracts requires a provider; no empty provider is substituted.

## Provider intervals

These settings belong to `ServiceDiscovery:Consul` and take effect after an application restart.

| Setting | Purpose |
| --- | --- |
| `DnsTimeout`, `DnsMaxAttempts` | Timeout of one DNS attempt and number of attempts per DNS server |
| `DnsRefreshPeriod` | DNS refresh period when the response has no usable TTL |
| `DnsMinRetryPeriod`, `DnsMaxRetryPeriod`, `DnsRetryBackOffFactor` | Resolution retry delays and multiplier |
| `Ttl`, `HeartbeatInterval` | Instance check TTL and renewal interval |
| `RequestTimeout`, `RegistrationRetryInterval` | Agent HTTP request timeout and registration recovery interval |
| `DeregisterCriticalServiceAfter` | Consul's delay before removing a critical registration |

Intervals and attempt counts must be positive, heartbeat must be shorter than TTL, and the maximum DNS retry delay must not be shorter than the initial delay. Excluding a dead instance from healthy DNS results and physically deleting its record are distinct events. RPC retries are configured separately. `DnsTimeout` does not bound the entire RPC: one DNS query may use several attempts and servers in sequence. Bound the call itself with a deadline or cancellation.

## Default and contract-specific limits

Limits are expressed in bytes. This configuration allows clients and servers
to receive up to 1 MiB by default and up to 4 MiB for `accounting.v1.Invoice`:

```json
{
  "Grpc": {
    "Client": {
      "Default": { "MaxReceiveMessageSize": 1048576 },
      "Contracts": {
        "accounting.v1.Invoice": { "MaxReceiveMessageSize": 4194304 }
      }
    },
    "Server": {
      "Default": { "MaxReceiveMessageSize": 1048576 },
      "Contracts": {
        "accounting.v1.Invoice": { "MaxReceiveMessageSize": 4194304 }
      }
    }
  }
}
```

`MaxSendMessageSize` limits sending independently. Both the sender's and receiver's
limits must permit the message. The limit applies to the protobuf message, so payload
at the exact boundary can exceed it once protocol fields are included.
`MaxReceiveMessageSize` limits both the compressed and decompressed message size.
If either exceeds the limit, the receiver returns `ResourceExhausted` before deserialization.

Settings apply in order: SDK values → `Default` → shared callback → `Contracts` →
individual callback. An omitted field preserves its previous value. Register a shared
client callback with `ConfigureGrpcClients(options => ...)` and an individual callback
with `AddGrpcClient<T>(options => ...)`.

Use `ConfigureGrpcService` for shared server SDK settings. The individual
`RegisterService` callback receives `GrpcServiceOptions` directly:

```csharp
app.AddGrpcServer(options =>
    options.ConfigureGrpcService = grpc => grpc.MaxReceiveMessageSize = 1048576)
    .RegisterService<InvoiceGrpcService>(grpc => grpc.MaxReceiveMessageSize = 4194304)
    .RegisterService<ReportsGrpcService>();
```

Repeated `AddGrpcServer` calls contribute to one server per application. Every
contract receives the final shared settings, including callbacks added after
service registration. Repeating the same type does not create another route;
a different implementation type for the same protobuf contract causes an error.
Use `RegisterServiceWithGrpcWeb<TService>()` for gRPC-Web.
`RequireSchemeAuthorization(...)` in shared server settings retains the required
authentication scheme.

Client `RetryPolicy` settings in `Default` or `Contracts` configure RPC retries:
`MaxAttempts`, `InitialBackoff`, `MaxBackoff`, `BackoffMultiplier` and
`RetryableStatusCodes`. An explicit code list replaces the previous list;
omitting it preserves the previous list. Other policy fields remain unchanged
unless overridden. For example, `"RetryableStatusCodes": ["Unavailable"]`
excludes retries on `ResourceExhausted`. RPC retries are separate from DNS and
registration retries.

## Reflection

`Grpc:Server:EnableReflection` defaults to `false`: normal RPCs work while reflection returns `Unimplemented`. Set it to `true` and restart the application to expose descriptions of registered services.

## TLS and tokens

TLS remains part of the gRPC transport. DNS selects an address and port; certificate configuration and trust remain application responsibilities. Existing `ConfigureHttpHandler`, `ConfigureChannelOptions`, token providers and metadata providers remain client extension points. `DisableCertificatesValidation` explicitly disables certificate validation when selected by the application.

`AddTokenAuth<TProvider>()` registers an `IGrpcTokenProvider`. The provider returns the entire `Authorization` header value, including its scheme, such as `Bearer …`, and is called for an RPC. Sending credentials over an insecure connection is not enabled automatically. Incoming server authorization is configured separately.

A shared policy also applies to clients registered before it:

```csharp
app.ConfigureGrpcClients(options => options.AddTokenAuth<ApplicationTokenProvider>());
app.AddGrpcClient<Invoice.InvoiceClient>();
app.AddGrpcClient<Reports.ReportsClient>(options => options.AddTokenAuth<ReportsTokenProvider>());
```

The providers in this example implement `IGrpcTokenProvider`. An individual provider
replaces the shared provider. To select its lifetime, register the concrete provider
type in DI first, for example `services.AddScoped<ApplicationTokenProvider>()`;
`AddTokenAuth` preserves that registration. Otherwise, it uses transient lifetime.
The provider is resolved from the client's DI scope for every RPC, so a token change
does not require a new client or channel. An explicit
`IGrpcTokenProviderFactory<TClient>` takes priority over the shared provider.

`ConfigureGrpcClients` also supports shared `AddMetadataProvider`, `AddInterceptor`,
`ConfigureHttpHandler` and `ConfigureChannelOptions` settings.

Implementation entry points: [gRPC client](../../src/Sitko.Core.Grpc.Client/GrpcClientModule.cs), [server](../../src/Sitko.Core.Grpc.Server/GrpcServerModule.cs), [Consul provider](../../src/Sitko.Core.ServiceDiscovery.Consul/ConsulServiceDiscoveryModule.cs). The migration contract is recorded in the [specification](../../specs/20261027-193928-service-discovery/spec.md).

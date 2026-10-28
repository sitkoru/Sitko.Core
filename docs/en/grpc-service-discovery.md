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
| `DnsTimeout`, `DnsMaxAttempts` | Timeout of one DNS attempt and number of attempts |
| `DnsRefreshPeriod` | DNS refresh period when the response has no usable TTL |
| `DnsMinRetryPeriod`, `DnsMaxRetryPeriod`, `DnsRetryBackOffFactor` | Resolution retry delays and multiplier |
| `Ttl`, `HeartbeatInterval` | Instance check TTL and renewal interval |
| `RequestTimeout`, `RegistrationRetryInterval` | Agent HTTP request timeout and registration recovery interval |
| `DeregisterCriticalServiceAfter` | Consul's delay before removing a critical registration |

Intervals and attempt counts must be positive, heartbeat must be shorter than TTL, and the maximum DNS retry delay must not be shorter than the initial delay. Excluding a dead instance from healthy DNS results and physically deleting its record are distinct events. RPC retries are configured separately.

## TLS and tokens

TLS remains part of the gRPC transport. DNS selects an address and port; certificate configuration and trust remain application responsibilities. Existing `ConfigureHttpHandler`, `ConfigureChannelOptions`, token providers and metadata providers remain client extension points. `DisableCertificatesValidation` explicitly disables certificate validation when selected by the application.

`AddTokenAuth<TProvider>()` registers an `IGrpcTokenProvider`. The provider returns the entire `Authorization` header value, including its scheme, such as `Bearer …`, and is called for an RPC. Sending credentials over an insecure connection is not enabled automatically. Incoming server authorization is configured separately.

Implementation entry points: [gRPC client](../../src/Sitko.Core.Grpc.Client/GrpcClientModule.cs), [server](../../src/Sitko.Core.Grpc.Server/GrpcServerModule.cs), [Consul provider](../../src/Sitko.Core.ServiceDiscovery.Consul/ConsulServiceDiscoveryModule.cs). The migration contract is recorded in the [specification](../../specs/20261027-193928-service-discovery/spec.md).

﻿using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote.Wasm;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddWasmHttpRepositoryTransport(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddWasmHttpRepositoryTransport(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddWasmHttpRepositoryTransport(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, HttpRepositoryTransportOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddWasmHttpRepositoryTransport(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddWasmHttpRepositoryTransport(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<WasmHttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,
            optionsKey);

    public static SitkoCoreApplicationBuilder AddWasmHttpRepositoryTransport(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, HttpRepositoryTransportOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<WasmHttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure,
            optionsKey);
}
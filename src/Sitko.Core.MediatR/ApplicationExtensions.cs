﻿using Sitko.Core.App;

namespace Sitko.Core.MediatR;

public static class ApplicationExtensions
{
    public static Application AddMediatR<TAssembly>(this Application application,
        Action<IApplicationContext, MediatRModuleOptions<TAssembly>> configure,
        string? optionsKey = null) =>
        application.AddModule<MediatRModule<TAssembly>, MediatRModuleOptions<TAssembly>>(configure,
            optionsKey);

    public static Application AddMediatR<TAssembly>(this Application application,
        Action<MediatRModuleOptions<TAssembly>>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<MediatRModule<TAssembly>, MediatRModuleOptions<TAssembly>>(configure,
            optionsKey);
}


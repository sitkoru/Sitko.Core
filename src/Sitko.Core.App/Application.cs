// using System.Text.Json;
// using JetBrains.Annotations;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
//
// namespace Sitko.Core.App;
//
// public abstract class Application : IApplication
// {
//     // private readonly List<ApplicationModuleRegistration> moduleRegistrations =
//     //     new();
//
//     //private readonly Dictionary<string, object> store = new();
//     //private bool disposed;
//
//     protected Application(string[] args)
//     {
//         //Args = args;
//         //AddModule<CommandsModule>();
//     }
//
//
//     protected string[] Args { get; set; }
//
//     public static string OptionsKey => nameof(Application);
//
//     public Guid Id { get; } = Guid.NewGuid();
//
//
//     // public string Name => GetApplicationOptions().Name;
//     // public string Version => GetApplicationOptions().Version;
//
//     // public async ValueTask DisposeAsync()
//     // {
//     //     if (disposed)
//     //     {
//     //         return;
//     //     }
//     //
//     //     await DisposeAsync(true);
//     //     GC.SuppressFinalize(this);
//     //     disposed = true;
//     // }
//
//     protected virtual ValueTask DisposeAsync(bool disposing) => new();
//
//     [PublicAPI]
//     //public ApplicationOptions GetApplicationOptions() => GetContext().Options;
//
//
//
//     protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
//     {
//     }
//
//     protected virtual void ConfigureAppConfiguration(HostBuilderContext context,
//         IConfigurationBuilder configurationBuilder)
//     {
//     }
//
//
//     [PublicAPI]
//     //public Dictionary<string, object> GetModulesOptions() => GetModulesOptions(GetContext());
//
//
//
//     public async Task RunAsync()
//     {
//         // LogInternal("Run app start");
//         // LogInternal("Build and init");
//         // var context = await BuildAppContextAsync();
//
//         // var enabledModules = GetEnabledModuleRegistrations(context).ToArray();
//         // foreach (var enabledModule in enabledModules)
//         // {
//         //     var shouldContinue = await enabledModule.GetInstance().OnBeforeRunAsync(this, context, Args);
//         //     if (!shouldContinue)
//         //     {
//         //         return;
//         //     }
//         // }
//
//         // LogInternal("Check required modules");
//         // var modulesCheckSuccess = true;
//         // foreach (var registration in enabledModules)
//         // {
//         //     var result =
//         //         registration.CheckRequiredModules(context,
//         //             enabledModules.Select(r => r.Type).ToArray());
//         //     if (!result.isSuccess)
//         //     {
//         //         foreach (var missingModule in result.missingModules)
//         //         {
//         //             LogInternal($"Required module {missingModule} for module {registration.Type} is not registered");
//         //         }
//         //
//         //         modulesCheckSuccess = false;
//         //     }
//         // }
//
//         // if (!modulesCheckSuccess)
//         // {
//         //     LogInternal("Check required modules failed");
//         //     return;
//         // }
//
//         // foreach (var enabledModule in enabledModules)
//         // {
//         //     var shouldContinue = await enabledModule.GetInstance().OnAfterRunAsync(this, context, Args);
//         //     if (!shouldContinue)
//         //     {
//         //         return;
//         //     }
//         // }
//
//         //await DoRunAsync();
//     }
//
//     protected abstract Task DoRunAsync();
//
//     protected abstract Task<IApplicationContext> BuildAppContextAsync();
//
//     public abstract Task StopAsync();
//
//     // protected async Task InitAsync(IServiceProvider serviceProvider)
//     // {
//     //     LogInternal("Build and init async start");
//     //     using var scope = serviceProvider.CreateScope();
//     //     var logger = scope.ServiceProvider.GetRequiredService<ILogger<Application>>();
//     //     logger.LogInformation("Init modules");
//     //     // var registrations = GetEnabledModuleRegistrations(GetContext(scope.ServiceProvider));
//     //     // var context = GetContext(scope.ServiceProvider);
//     //     // foreach (var configurationModule in registrations.Select(module => module.GetInstance())
//     //     //              .OfType<IConfigurationModule>())
//     //     // {
//     //     //     configurationModule.CheckConfiguration(context, scope.ServiceProvider);
//     //     // }
//     //     //
//     //     // foreach (var registration in registrations)
//     //     // {
//     //     //     logger.LogInformation("Init module {Module}", registration.Type);
//     //     //     await registration.InitAsync(context, scope.ServiceProvider);
//     //     // }
//     //
//     //     LogInternal("Build and init async done");
//     // }
//
//     protected virtual void LogInternal(string message) { }
//
//     protected abstract bool CanAddModule();
//
//     // [PublicAPI]
//     // protected void RegisterModule<TModule, TModuleOptions>(
//     //     Action<IApplicationContext, TModuleOptions>? configureOptions = null,
//     //     string? optionsKey = null)
//     //     where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
//     // {
//     //     if (!CanAddModule())
//     //     {
//     //         throw new InvalidOperationException("App host is already built. Can't add modules after it");
//     //     }
//     //
//     //     var instance = new TModule();
//     //     if (!instance.AllowMultiple && HasModule<TModule>())
//     //     {
//     //         throw new InvalidOperationException($"Module {typeof(TModule)} already registered");
//     //     }
//     //
//     //     moduleRegistrations.Add(
//     //         new ApplicationModuleRegistration<TModule, TModuleOptions>(instance, configureOptions, optionsKey));
//     // }
//
//     // protected virtual void InitApplication()
//     // {
//     // }
//
//
//     // [PublicAPI]
//     // protected abstract IApplicationContext GetContext();
//     //
//     // [PublicAPI]
//     // protected abstract IApplicationContext GetContext(IServiceProvider serviceProvider);
//
//
//
//
//     public Application AddModule<TModule>() where TModule : BaseApplicationModule, new()
//
//     {
//         //RegisterModule<TModule, BaseApplicationModuleOptions>();
//         return this;
//     }
//
//     public Application AddModule<TModule, TModuleOptions>(
//         Action<IApplicationContext, TModuleOptions> configureOptions,
//         string? optionsKey = null)
//         where TModule : IApplicationModule<TModuleOptions>, new()
//         where TModuleOptions : BaseModuleOptions, new()
//     {
//         //RegisterModule<TModule, TModuleOptions>(configureOptions, optionsKey);
//         return this;
//     }
//
//     public Application AddModule<TModule, TModuleOptions>(
//         Action<TModuleOptions>? configureOptions = null,
//         string? optionsKey = null)
//         where TModule : IApplicationModule<TModuleOptions>, new()
//         where TModuleOptions : BaseModuleOptions, new() =>
//         AddModule<TModule, TModuleOptions>((_, moduleOptions) =>
//         {
//             configureOptions?.Invoke(moduleOptions);
//         }, optionsKey);
// }

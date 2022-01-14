using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sitko.Core.App;

public abstract class BaseApplicationModule : BaseApplicationModule<BaseApplicationModuleOptions>
{
}

public class BaseApplicationModuleOptions : BaseModuleOptions
{
}

public abstract class BaseApplicationModule<TModuleOptions> : IApplicationModule<TModuleOptions>
    where TModuleOptions : BaseModuleOptions, new()
{
    public virtual void ConfigureServices(IApplicationContext context, IServiceCollection services,
        TModuleOptions startupOptions)
    {
    }

    public virtual Task InitAsync(IApplicationContext context, IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public virtual IEnumerable<Type> GetRequiredModules(IApplicationContext context, TModuleOptions options) =>
        Type.EmptyTypes;

    public virtual Task ApplicationStarted(IApplicationContext applicationContext,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public virtual Task ApplicationStopping(IApplicationContext applicationContext,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public virtual Task ApplicationStopped(IApplicationContext applicationContext,
        IServiceProvider serviceProvider) =>
        Task.CompletedTask;

    public virtual Task<bool> OnBeforeRunAsync(Application application, IApplicationContext applicationContext,
        string[] args) => Task.FromResult(true);

    public virtual Task<bool> OnAfterRunAsync(Application application, IApplicationContext applicationContext,
        string[] args) => Task.FromResult(true);

    public abstract string OptionsKey { get; }
    public virtual bool AllowMultiple => false;

    public TModuleOptions GetOptions(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<IOptions<TModuleOptions>>().Value;
}

public interface IModuleOptionsWithValidation
{
    Type GetValidatorType();
}

public abstract class BaseModuleOptions
{
    public virtual bool Enabled { get; set; } = true;

    public virtual void Configure(IApplicationContext applicationContext)
    {
    }
}
